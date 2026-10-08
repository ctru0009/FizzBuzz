import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Mock } from "vitest";

vi.mock("@/const", () => ({
  BACKEND_URL: "http://backend.test/api",
  SIGNALR_URL: "http://backend.test/sessionHub",
}));

import { ApiError, apiFetch, getJson, postJson } from "./api";
import { BACKEND_URL } from "@/const";

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

interface StubbedLocation {
  assign: Mock;
}

function stubLocation(pathname: string): StubbedLocation {
  const assign = vi.fn();
  Object.defineProperty(window, "location", {
    value: { pathname, search: "", assign },
    writable: true,
  });
  return { assign };
}

describe("api client", () => {
  const originalLocation = window.location;

  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  afterEach(() => {
    Object.defineProperty(window, "location", {
      value: originalLocation,
      writable: true,
    });
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    vi.useRealTimers();
  });

  it("aborts a slow request with a timeout error", async () => {
    vi.useFakeTimers();
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockImplementationOnce((_url, init) => {
      const { promise, reject } = Promise.withResolvers<Response>();
      init?.signal?.addEventListener("abort", () => {
        reject(new DOMException("The operation was aborted.", "AbortError"));
      });
      return promise;
    });

    const pending = apiFetch("/games", { method: "GET" });
    const assertion = expect(pending).rejects.toMatchObject({
      status: 0,
      detail: "Request timed out.",
    });
    await vi.advanceTimersByTimeAsync(10_001);
    await assertion;
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("redirects to login with the returnUrl on 401", async () => {
    const { assign } = stubLocation("/play/7");
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse({ detail: "Unauthorized." }, 401),
    );

    const error = await apiFetch("/players/me", { method: "GET" }).catch(
      (caught: unknown) => caught,
    );
    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 401, detail: "Unauthorized." });
    expect(assign).toHaveBeenCalledTimes(1);
    expect(assign).toHaveBeenCalledWith("/?returnUrl=%2Fplay%2F7");
  });

  it("skips the redirect when already on the login page", async () => {
    const { assign } = stubLocation("/");
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse({ detail: "Unauthorized." }, 401),
    );

    const error = await apiFetch("/players/me", { method: "GET" }).catch(
      (caught: unknown) => caught,
    );
    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 401 });
    expect(assign).not.toHaveBeenCalled();
  });

  it("attaches the antiforgery header on non exempt mutations", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(jsonResponse({ token: "csrf-token" }));
    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 3 }));

    await postJson("/games", { name: "Speed round" });

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      `${BACKEND_URL}/antiforgery/token`,
    );
    const mutationInit = fetchMock.mock.calls[1]?.[1];
    const headers = new Headers(mutationInit?.headers);
    expect(headers.get("X-XSRF-TOKEN")).toBe("csrf-token");
    expect(headers.get("Content-Type")).toBe("application/json");
    expect(mutationInit).toMatchObject({
      method: "POST",
      credentials: "include",
    });
  });

  it("exempts register, login, and logout from the antiforgery header", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 1, name: "Ada" }));
    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 1, name: "Ada" }));
    fetchMock.mockResolvedValueOnce(jsonResponse({}));

    await postJson("/players/register", { name: "Ada", password: "secret-99" });
    await postJson("/players/login", { name: "Ada", password: "secret-99" });
    await postJson("/players/logout", {});

    expect(fetchMock).toHaveBeenCalledTimes(3);
    for (const call of fetchMock.mock.calls) {
      expect(call[0]).not.toContain("/antiforgery/token");
      const headers = new Headers(call[1]?.headers);
      expect(headers.get("X-XSRF-TOKEN")).toBeNull();
    }
  });

  it("parses ProblemDetails detail, title, and validation errors in order", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(
      jsonResponse({ detail: "Name is taken.", title: "Conflict" }, 409),
    );
    fetchMock.mockResolvedValueOnce(
      jsonResponse({ title: "Bad request." }, 400),
    );
    fetchMock.mockResolvedValueOnce(
      jsonResponse(
        { errors: { name: ["Name is required."], password: [] } },
        400,
      ),
    );

    await expect(getJson("/games")).rejects.toMatchObject({
      status: 409,
      detail: "Name is taken.",
    });
    await expect(getJson("/games")).rejects.toMatchObject({
      status: 400,
      detail: "Bad request.",
    });
    await expect(getJson("/games")).rejects.toMatchObject({
      status: 400,
      detail: "Name is required.",
    });
  });

  it("falls back to status text for empty and non JSON error bodies", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(new Response("", { status: 500 }));
    fetchMock.mockResolvedValueOnce(
      new Response("bad gateway", {
        status: 502,
        headers: { "Content-Type": "text/plain" },
      }),
    );

    await expect(getJson("/games")).rejects.toMatchObject({
      status: 500,
      detail: "Request failed with status 500",
    });
    await expect(getJson("/games")).rejects.toMatchObject({
      status: 502,
      detail: "bad gateway",
    });
  });

  it("rejects invalid JSON on a successful response", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response("not json", { status: 200 }),
    );

    await expect(getJson("/games")).rejects.toMatchObject({
      status: 200,
      detail: "Server returned an invalid response.",
    });
  });

  it("sends credentials on GET requests", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(jsonResponse([]));

    await getJson("/games");

    expect(fetchMock).toHaveBeenCalledWith(`${BACKEND_URL}/games`, {
      method: "GET",
      headers: expect.any(Headers),
      credentials: "include",
      signal: expect.any(AbortSignal),
    });
  });
});
