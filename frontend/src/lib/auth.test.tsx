import { act, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/api", () => ({
  ApiError: class ApiError extends Error {
    status: number;
    detail: string;
    constructor(status: number, detail: string) {
      super(detail);
      this.name = "ApiError";
      this.status = status;
      this.detail = detail;
    }
  },
  apiFetch: vi.fn(),
  getJson: vi.fn(),
  postJson: vi.fn(),
}));

import { ApiError, getJson, postJson } from "@/lib/api";
import { AuthProvider, useAuth } from "./auth";

const mockedGetJson = vi.mocked(getJson);
const mockedPostJson = vi.mocked(postJson);

function Probe() {
  const { player, loading, login, register, logout } = useAuth();
  return (
    <div>
      <p>{loading ? "loading" : "ready"}</p>
      <p>player: {player ? `${player.id}:${player.name}` : "none"}</p>
      <button onClick={() => void login("Ada", "secret-99")}>login</button>
      <button onClick={() => void register("Ada", "secret-99")}>
        register
      </button>
      <button onClick={() => void logout()}>logout</button>
    </div>
  );
}

function renderProbe(): void {
  render(
    <AuthProvider>
      <Probe />
    </AuthProvider>,
  );
}

beforeEach(() => {
  mockedGetJson.mockReset();
  mockedPostJson.mockReset();
});

describe("AuthProvider", () => {
  it("loads the signed in player on mount", async () => {
    mockedGetJson.mockResolvedValueOnce({ id: 4, name: "Ada" });
    renderProbe();

    expect(screen.getByText("loading")).toBeInTheDocument();
    await waitFor(() =>
      expect(screen.getByText("player: 4:Ada")).toBeInTheDocument(),
    );
    expect(screen.getByText("ready")).toBeInTheDocument();
    expect(mockedGetJson).toHaveBeenCalledWith("/players/me");
  });

  it("treats a failed me lookup as signed out", async () => {
    mockedGetJson.mockRejectedValueOnce(new ApiError(401, "Unauthorized."));
    renderProbe();

    await waitFor(() =>
      expect(screen.getByText("player: none")).toBeInTheDocument(),
    );
    expect(screen.getByText("ready")).toBeInTheDocument();
  });

  it("login sets the player from the response", async () => {
    mockedGetJson.mockResolvedValueOnce({ id: 0, name: "" });
    mockedPostJson.mockResolvedValueOnce({ id: 5, name: "Ada" });
    renderProbe();
    await waitFor(() => expect(screen.getByText("ready")).toBeInTheDocument());

    await act(async () => {
      screen.getByRole("button", { name: "login" }).click();
    });

    await waitFor(() =>
      expect(screen.getByText("player: 5:Ada")).toBeInTheDocument(),
    );
    expect(mockedPostJson).toHaveBeenCalledWith("/players/login", {
      name: "Ada",
      password: "secret-99",
    });
  });

  it("register sets the player from the response", async () => {
    mockedGetJson.mockResolvedValueOnce({ id: 0, name: "" });
    mockedPostJson.mockResolvedValueOnce({ id: 6, name: "Grace" });
    renderProbe();
    await waitFor(() => expect(screen.getByText("ready")).toBeInTheDocument());

    await act(async () => {
      screen.getByRole("button", { name: "register" }).click();
    });

    await waitFor(() =>
      expect(screen.getByText("player: 6:Grace")).toBeInTheDocument(),
    );
    expect(mockedPostJson).toHaveBeenCalledWith("/players/register", {
      name: "Ada",
      password: "secret-99",
    });
  });

  it("logout clears the player even when the request fails", async () => {
    mockedGetJson.mockResolvedValueOnce({ id: 7, name: "Ada" });
    mockedPostJson.mockRejectedValueOnce(new ApiError(0, "Network down."));
    renderProbe();
    await waitFor(() =>
      expect(screen.getByText("player: 7:Ada")).toBeInTheDocument(),
    );

    await act(async () => {
      screen.getByRole("button", { name: "logout" }).click();
    });

    await waitFor(() =>
      expect(screen.getByText("player: none")).toBeInTheDocument(),
    );
    expect(mockedPostJson).toHaveBeenCalledWith("/players/logout", {});
  });

  it("broadcasts auth changes to other mounted providers", async () => {
    mockedGetJson.mockResolvedValue({ id: 0, name: "" });
    mockedPostJson.mockResolvedValueOnce({ id: 8, name: "Ada" });
    render(
      <div>
        <AuthProvider>
          <Probe />
        </AuthProvider>
        <AuthProvider>
          <Probe />
        </AuthProvider>
      </div>,
    );
    await waitFor(() => expect(screen.getAllByText("ready")).toHaveLength(2));

    await act(async () => {
      screen.getAllByRole("button", { name: "login" })[0]?.click();
    });

    await waitFor(() =>
      expect(screen.getAllByText("player: 8:Ada")).toHaveLength(2),
    );
  });
});
