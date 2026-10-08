import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

const { routerMock } = vi.hoisted(() => ({
  routerMock: { push: vi.fn(), replace: vi.fn() },
}));

vi.mock("next/navigation", () => ({
  useRouter: () => routerMock,
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

import { ApiError, getJson } from "@/lib/api";
import GameList from "./GameList";

const mockedGetJson = vi.mocked(getJson);

const games = [
  {
    id: 1,
    name: "Classic",
    authorName: "Ada",
    startRange: 1,
    endRange: 100,
    createdAt: "2026-01-02T00:00:00Z",
    rules: [{ divisibleBy: 3, replacementWord: "Fizz" }],
  },
  {
    id: 2,
    name: "Speed",
    authorName: "Grace",
    startRange: 1,
    endRange: 50,
    createdAt: "2026-01-01T00:00:00Z",
    rules: [{ divisibleBy: 5, replacementWord: "Buzz" }],
  },
];

const openSessions = [
  {
    id: 11,
    gameId: 1,
    gameName: "Classic",
    playerCount: 2,
    endsAtUtc: "2026-10-08T12:00:00Z",
  },
  {
    id: 12,
    gameId: 2,
    gameName: "Speed",
    playerCount: 1,
    endsAtUtc: "2026-10-08T12:05:00Z",
  },
];

beforeEach(() => {
  mockedGetJson.mockReset();
  routerMock.push.mockReset();
  routerMock.replace.mockReset();
});

describe("GameList open sessions", () => {
  it("renders open sessions with join navigation", async () => {
    mockedGetJson.mockImplementation((path: string) => {
      if (path === "/games") {
        return Promise.resolve(games);
      }
      if (path === "/sessions/open") {
        return Promise.resolve(openSessions);
      }
      return Promise.reject(new ApiError(404, "Not found."));
    });
    const user = userEvent.setup();
    render(<GameList />);

    expect(screen.getByText("Loading games...")).toBeInTheDocument();
    expect(await screen.findByText("Open sessions")).toBeInTheDocument();
    expect(screen.getByText("2 players playing")).toBeInTheDocument();
    expect(screen.getByText("1 player playing")).toBeInTheDocument();

    const joinButtons = screen.getAllByRole("button", { name: "Join" });
    expect(joinButtons).toHaveLength(2);
    const firstJoin = joinButtons[0];
    expect(firstJoin).toBeDefined();
    if (firstJoin) {
      await user.click(firstJoin);
    }
    expect(routerMock.push).toHaveBeenCalledWith("/play/11");
  });

  it("shows an empty state when no sessions are open", async () => {
    mockedGetJson.mockImplementation((path: string) => {
      if (path === "/games") {
        return Promise.resolve(games);
      }
      if (path === "/sessions/open") {
        return Promise.resolve([]);
      }
      return Promise.reject(new ApiError(404, "Not found."));
    });
    render(<GameList />);

    expect(
      await screen.findByText(
        "No open sessions right now. Start one from a game below.",
      ),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Join" }),
    ).not.toBeInTheDocument();
  });

  it("keeps the games list when open sessions fail to load", async () => {
    mockedGetJson.mockImplementation((path: string) => {
      if (path === "/games") {
        return Promise.resolve(games);
      }
      return Promise.reject(new ApiError(500, "Open sessions unavailable."));
    });
    render(<GameList />);

    expect(await screen.findByText("Open sessions")).toBeInTheDocument();
    expect(
      await screen.findByText("Error: Open sessions unavailable."),
    ).toBeInTheDocument();
    expect(await screen.findByText("Classic")).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Join" }),
    ).not.toBeInTheDocument();
  });

  it("sorts games and opens the create page", async () => {
    mockedGetJson.mockImplementation((path: string) => {
      if (path === "/games") {
        return Promise.resolve(games);
      }
      if (path === "/sessions/open") {
        return Promise.resolve([]);
      }
      return Promise.reject(new ApiError(404, "Not found."));
    });
    const user = userEvent.setup();
    render(<GameList />);
    await screen.findByText("Open sessions");

    const headings = screen.getAllByRole("heading", { level: 2 });
    expect(headings[1]).toHaveTextContent("Classic");
    await user.selectOptions(screen.getByLabelText("Sort by:"), "createdAt");
    await waitFor(() => {
      const resorted = screen.getAllByRole("heading", { level: 2 });
      expect(resorted[1]).toHaveTextContent("Speed");
    });
    await user.click(screen.getByRole("button", { name: "Create New Game" }));
    expect(routerMock.push).toHaveBeenCalledWith("/games/create");
  });

  it("shows a games error when the games request fails", async () => {
    mockedGetJson.mockImplementation((path: string) => {
      if (path === "/sessions/open") {
        return Promise.resolve([]);
      }
      return Promise.reject(new ApiError(500, "Games unavailable."));
    });
    render(<GameList />);

    expect(
      await screen.findByText("Error: Games unavailable."),
    ).toBeInTheDocument();
  });
});
