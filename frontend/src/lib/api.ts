import { BACKEND_URL } from "@/const";

const REQUEST_TIMEOUT_MS = 10_000;

export class ApiError extends Error {
  status: number;
  detail: string;

  constructor(status: number, detail: string) {
    super(detail);
    this.name = "ApiError";
    this.status = status;
    this.detail = detail;
  }
}

interface ProblemDetails {
  detail?: unknown;
  title?: unknown;
  errors?: unknown;
}

interface AntiforgeryTokenResponse {
  token?: unknown;
}

function isExemptMutationPath(suffix: string): boolean {
  const normalized = suffix.split(/[?#]/, 1)[0].toLowerCase();
  return (
    normalized === "/players/register" ||
    normalized === "/players/login" ||
    normalized === "/players/logout"
  );
}

let cachedToken: string | null = null;
let tokenRequest: Promise<string | null> | null = null;

async function getAntiforgeryToken(): Promise<string | null> {
  if (cachedToken) {
    return cachedToken;
  }
  if (!tokenRequest) {
    tokenRequest = (async () => {
      try {
        const response = await fetch(`${BACKEND_URL}/antiforgery/token`, {
          credentials: "include",
        });
        if (!response.ok) {
          return null;
        }
        const body = (await response.json()) as AntiforgeryTokenResponse;
        if (typeof body?.token === "string" && body.token.length > 0) {
          cachedToken = body.token;
          return cachedToken;
        }
        return null;
      } catch {
        return null;
      } finally {
        tokenRequest = null;
      }
    })();
  }
  return tokenRequest;
}

function redirectToLogin(): void {
  if (typeof window === "undefined") {
    return;
  }
  if (window.location.pathname === "/") {
    return;
  }
  const current = `${window.location.pathname}${window.location.search}`;
  window.location.assign(`/?returnUrl=${encodeURIComponent(current)}`);
}

async function readDetail(response: Response): Promise<string> {
  const fallback = `Request failed with status ${response.status}`;
  try {
    const text = await response.text();
    if (!text) {
      return fallback;
    }
    try {
      const body = JSON.parse(text) as ProblemDetails;
      if (body && typeof body === "object") {
        if (typeof body.detail === "string" && body.detail.length > 0) {
          return body.detail;
        }
        if (typeof body.title === "string" && body.title.length > 0) {
          return body.title;
        }
        if (body.errors && typeof body.errors === "object") {
          const messages = Object.values(body.errors as Record<string, unknown>)
            .flat()
            .filter(
              (message): message is string =>
                typeof message === "string" && message.length > 0,
            );
          if (messages.length > 0) {
            return messages.join(" ");
          }
        }
      }
      return text.slice(0, 500);
    } catch {
      return text.slice(0, 500);
    }
  } catch {
    return fallback;
  }
}

export async function apiFetch(
  path: string,
  init?: RequestInit,
): Promise<Response> {
  const suffix = path.startsWith("/") ? path : `/${path}`;
  const url = `${BACKEND_URL}${suffix}`;
  const method = (init?.method ?? "GET").toUpperCase();

  const headers = new Headers(init?.headers);
  if (typeof init?.body === "string" && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }
  if (
    (method === "POST" ||
      method === "PUT" ||
      method === "PATCH" ||
      method === "DELETE") &&
    !isExemptMutationPath(suffix)
  ) {
    const token = await getAntiforgeryToken();
    if (token) {
      headers.set("X-XSRF-TOKEN", token);
    }
  }

  const controller = new AbortController();
  let timedOut = false;
  const timer = setTimeout(() => {
    timedOut = true;
    controller.abort();
  }, REQUEST_TIMEOUT_MS);
  const callerSignal = init?.signal;
  const onCallerAbort = () => controller.abort();
  try {
    if (callerSignal) {
      if (callerSignal.aborted) {
        controller.abort();
      } else {
        callerSignal.addEventListener("abort", onCallerAbort, { once: true });
      }
    }
    const response = await fetch(url, {
      ...init,
      method,
      headers,
      credentials: "include",
      signal: controller.signal,
    });
    if (response.status === 401) {
      const detail = await readDetail(response);
      redirectToLogin();
      throw new ApiError(401, detail);
    }
    if (!response.ok) {
      throw new ApiError(response.status, await readDetail(response));
    }
    return response;
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }
    if (timedOut) {
      throw new ApiError(0, "Request timed out.");
    }
    throw new ApiError(
      0,
      error instanceof Error ? error.message : "Network request failed.",
    );
  } finally {
    clearTimeout(timer);
    callerSignal?.removeEventListener("abort", onCallerAbort);
  }
}

async function parseJson<T>(response: Response): Promise<T> {
  const text = await response.text();
  if (!text) {
    return undefined as T;
  }
  try {
    return JSON.parse(text) as T;
  } catch {
    throw new ApiError(response.status, "Server returned an invalid response.");
  }
}

export async function getJson<T>(path: string): Promise<T> {
  const response = await apiFetch(path, { method: "GET" });
  return parseJson<T>(response);
}

export async function postJson<T>(path: string, body: unknown): Promise<T> {
  const response = await apiFetch(path, {
    method: "POST",
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  return parseJson<T>(response);
}
