import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { UserEvent } from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

const { authState, routerMock, searchParams } = vi.hoisted(() => ({
  authState: {
    player: null as { id: number; name: string } | null,
    loading: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
  },
  routerMock: { push: vi.fn(), replace: vi.fn() },
  searchParams: new URLSearchParams(),
}));

vi.mock("@/lib/auth", () => ({
  AuthProvider: ({ children }: { children: React.ReactNode }) => (
    <div>{children}</div>
  ),
  useAuth: () => authState,
}));

vi.mock("next/navigation", () => ({
  useRouter: () => routerMock,
  useSearchParams: () => searchParams,
}));

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

import PlayerGreeting from "./PlayerGreeting";
import { ApiError } from "@/lib/api";

async function fillValidForm(user: UserEvent) {
  await user.type(screen.getByLabelText("Name"), "Ada");
  await user.type(screen.getByLabelText("Password"), "secret-99");
}

beforeEach(() => {
  authState.player = null;
  authState.loading = false;
  authState.login.mockReset().mockResolvedValue(undefined);
  authState.register.mockReset().mockResolvedValue(undefined);
  authState.logout.mockReset();
  routerMock.push.mockReset();
  routerMock.replace.mockReset();
});

describe("PlayerGreeting", () => {
  it("shows a checking state while auth loads", () => {
    authState.loading = true;
    render(<PlayerGreeting />);
    expect(screen.getByText("Checking sign in...")).toBeInTheDocument();
  });

  it("toggles between login and register modes", async () => {
    const user = userEvent.setup();
    render(<PlayerGreeting />);

    expect(
      screen.getByPlaceholderText("Enter your password"),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Register" }));
    expect(
      screen.getByPlaceholderText("8 to 100 characters"),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Create account" }),
    ).toBeInTheDocument();
    const loginTab = screen.getAllByRole("button", { name: "Login" })[0];
    expect(loginTab).toBeDefined();
    if (loginTab) {
      await user.click(loginTab);
    }
    expect(
      screen.getByPlaceholderText("Enter your password"),
    ).toBeInTheDocument();
  });

  it("shows validation messages for a short name and password", async () => {
    const user = userEvent.setup();
    render(<PlayerGreeting />);

    await user.type(screen.getByLabelText("Name"), "Ada");
    await user.type(screen.getByLabelText("Password"), "short");
    const submit = screen.getAllByRole("button", { name: "Login" })[1];
    expect(submit).toBeDefined();
    if (submit) {
      await user.click(submit);
    }

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Password is required and must be 8 to 100 characters.",
    );
    expect(authState.login).not.toHaveBeenCalled();
  });

  it("logs in and resumes the requested returnUrl", async () => {
    searchParams.set("returnUrl", "/play/7");
    try {
      const user = userEvent.setup();
      render(<PlayerGreeting />);
      await fillValidForm(user);

      const submit = screen.getAllByRole("button", { name: "Login" })[1];
      expect(submit).toBeDefined();
      if (submit) {
        await user.click(submit);
      }

      await waitFor(() =>
        expect(authState.login).toHaveBeenCalledWith("Ada", "secret-99"),
      );
      await waitFor(() =>
        expect(routerMock.replace).toHaveBeenCalledWith("/play/7"),
      );
    } finally {
      searchParams.delete("returnUrl");
    }
  });

  it("falls back to games for an unsafe returnUrl", async () => {
    searchParams.set("returnUrl", "https://evil.test/pickup");
    try {
      const user = userEvent.setup();
      render(<PlayerGreeting />);
      await fillValidForm(user);

      const submit = screen.getAllByRole("button", { name: "Login" })[1];
      expect(submit).toBeDefined();
      if (submit) {
        await user.click(submit);
      }

      await waitFor(() =>
        expect(routerMock.replace).toHaveBeenCalledWith("/games"),
      );
    } finally {
      searchParams.delete("returnUrl");
    }
  });

  it("surfaces register failures from the server", async () => {
    const user = userEvent.setup();
    authState.register.mockRejectedValueOnce(
      new ApiError(409, "Name is taken."),
    );
    render(<PlayerGreeting />);

    await user.click(screen.getByRole("button", { name: "Register" }));
    await fillValidForm(user);
    await user.click(screen.getByRole("button", { name: "Create account" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Name is taken.",
    );
    expect(routerMock.replace).not.toHaveBeenCalled();
  });
});
