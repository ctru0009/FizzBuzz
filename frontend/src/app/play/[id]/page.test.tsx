import { act, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
  JoinSessionResult,
  NumberAdvancedPayload,
  ScoresUpdatedPayload,
  SessionEndedPayload,
} from "@/types/Session";

interface HubHandlers {
  onNumberAdvanced?: (_payload: NumberAdvancedPayload) => void;
  onScoresUpdated?: (_payload: ScoresUpdatedPayload) => void;
  onSessionEnded?: (_payload: SessionEndedPayload) => void;
}

const { routerMock, signalrMocks, connectionState } = vi.hoisted(() => {
  const handlers: HubHandlers = {};
  return {
    routerMock: { push: vi.fn(), replace: vi.fn() },
    signalrMocks: {
      createConnection: vi.fn(),
      onNumberAdvanced: vi.fn(),
      onReconnectedResume: vi.fn(),
      onScoresUpdated: vi.fn(),
      onSessionEnded: vi.fn(),
      stopConnection: vi.fn(),
    },
    connectionState: {
      handlers,
      invoke: vi.fn(),
      start: vi.fn(),
    },
  };
});

vi.mock("next/navigation", () => ({
  useParams: () => ({ id: "7" }),
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

vi.mock("@/lib/signalr", () => ({
  createConnection: (...args: unknown[]) =>
    signalrMocks.createConnection(...args),
  onNumberAdvanced: (...args: unknown[]) =>
    signalrMocks.onNumberAdvanced(...args),
  onReconnectedResume: (...args: unknown[]) =>
    signalrMocks.onReconnectedResume(...args),
  onScoresUpdated: (...args: unknown[]) =>
    signalrMocks.onScoresUpdated(...args),
  onSessionEnded: (...args: unknown[]) => signalrMocks.onSessionEnded(...args),
  stopConnection: (...args: unknown[]) => signalrMocks.stopConnection(...args),
}));

import { ApiError, getJson } from "@/lib/api";
import PlayPage from "./page";

const mockedGetJson = vi.mocked(getJson);

const snapshot = {
  id: 7,
  gameId: 1,
  status: "Open" as const,
  startTimeUtc: "2026-10-08T11:55:00Z",
  endTimeUtc: "2026-10-08T12:05:00Z",
  currentNumber: 9,
  currentRound: 2,
  scores: [{ playerId: 1, playerName: "Ada", score: 10 }],
};

const joinState: JoinSessionResult = {
  sessionId: 7,
  number: 9,
  round: 2,
  endsAtUtc: "2026-10-08T12:05:00Z",
  scores: [{ playerId: 1, playerName: "Ada", score: 10 }],
  answeredCurrentRound: false,
};

function emitNumberAdvanced(payload: NumberAdvancedPayload): void {
  const handler = connectionState.handlers.onNumberAdvanced;
  act(() => {
    handler?.(payload);
  });
}

function emitScoresUpdated(payload: ScoresUpdatedPayload): void {
  const handler = connectionState.handlers.onScoresUpdated;
  act(() => {
    handler?.(payload);
  });
}

function emitSessionEnded(payload: SessionEndedPayload): void {
  const handler = connectionState.handlers.onSessionEnded;
  act(() => {
    handler?.(payload);
  });
}

function useFakeConnection(): void {
  connectionState.handlers = {};
  connectionState.invoke.mockReset();
  connectionState.start.mockReset().mockResolvedValue(undefined);
  connectionState.invoke.mockImplementation((method: string) => {
    if (method === "JoinSession") {
      return Promise.resolve(joinState);
    }
    return Promise.reject(new Error(`Unexpected invoke ${method}.`));
  });

  const connection = {
    invoke: connectionState.invoke,
    start: connectionState.start,
  };
  signalrMocks.createConnection.mockReturnValue(connection);
  signalrMocks.onNumberAdvanced.mockImplementation(
    (
      _connection: unknown,
      handler: (_payload: NumberAdvancedPayload) => void,
    ) => {
      connectionState.handlers.onNumberAdvanced = handler;
      return () => {};
    },
  );
  signalrMocks.onScoresUpdated.mockImplementation(
    (
      _connection: unknown,
      handler: (_payload: ScoresUpdatedPayload) => void,
    ) => {
      connectionState.handlers.onScoresUpdated = handler;
      return () => {};
    },
  );
  signalrMocks.onSessionEnded.mockImplementation(
    (
      _connection: unknown,
      handler: (_payload: SessionEndedPayload) => void,
    ) => {
      connectionState.handlers.onSessionEnded = handler;
      return () => {};
    },
  );
  signalrMocks.onReconnectedResume.mockReturnValue(() => {});
  signalrMocks.stopConnection.mockResolvedValue(undefined);
}

beforeEach(() => {
  mockedGetJson.mockReset();
  mockedGetJson.mockResolvedValue(snapshot);
  routerMock.push.mockReset();
  routerMock.replace.mockReset();
  vi.clearAllMocks();
  useFakeConnection();
});

describe("play page", () => {
  it("renders the live number and leaderboard after joining", async () => {
    render(<PlayPage />);

    expect(await screen.findByText("FizzBuzz Live")).toBeInTheDocument();
    expect(screen.getByText("9")).toBeInTheDocument();
    expect(screen.getByText("Ada")).toBeInTheDocument();
    expect(screen.getByText("10")).toBeInTheDocument();
    expect(connectionState.invoke).toHaveBeenCalledWith("JoinSession", 7);
  });

  it("shows the submitted state with feedback after answering", async () => {
    connectionState.invoke.mockImplementation((method: string) => {
      if (method === "JoinSession") {
        return Promise.resolve(joinState);
      }
      if (method === "SubmitAnswer") {
        return Promise.resolve({ correct: true, score: 20 });
      }
      return Promise.reject(new Error(`Unexpected invoke ${method}.`));
    });
    const user = userEvent.setup();
    render(<PlayPage />);
    await screen.findByText("FizzBuzz Live");

    await user.type(screen.getByLabelText("Answer input field"), "Fizz");
    await user.click(screen.getByRole("button", { name: "Submit" }));

    expect(await screen.findByText("Answer submitted.")).toBeInTheDocument();
    expect(screen.getByText("Correct! Your score is 20.")).toBeInTheDocument();
    expect(
      screen.queryByLabelText("Answer input field"),
    ).not.toBeInTheDocument();
    expect(connectionState.invoke).toHaveBeenCalledWith(
      "SubmitAnswer",
      7,
      2,
      "Fizz",
    );
  });

  it("shows a countdown while waiting after a new number arrives", async () => {
    const fixedNow = new Date("2026-10-08T12:00:00Z").getTime();
    vi.spyOn(Date, "now").mockReturnValue(fixedNow);
    connectionState.invoke.mockImplementation((method: string) => {
      if (method === "JoinSession") {
        return Promise.resolve(joinState);
      }
      if (method === "SubmitAnswer") {
        return Promise.resolve({ correct: false, score: 10 });
      }
      return Promise.reject(new Error(`Unexpected invoke ${method}.`));
    });
    const user = userEvent.setup();
    render(<PlayPage />);
    await screen.findByText("FizzBuzz Live");

    await user.type(screen.getByLabelText("Answer input field"), "9");
    await user.click(screen.getByRole("button", { name: "Submit" }));
    await screen.findByText("Answer submitted.");
    expect(
      screen.getByText("Waiting for the next number..."),
    ).toBeInTheDocument();

    emitNumberAdvanced({
      number: 15,
      round: 3,
      endsAtUtc: "2026-10-08T12:05:00Z",
      advancesAtUtc: new Date(fixedNow + 12_000).toISOString(),
    });
    emitScoresUpdated({
      scores: [
        { playerId: 1, playerName: "Ada", score: 10 },
        { playerId: 2, playerName: "Grace", score: 20 },
      ],
    });

    await waitFor(() => expect(screen.getByText("15")).toBeInTheDocument());
    expect(screen.getByText("Grace")).toBeInTheDocument();
    await user.type(screen.getByLabelText("Answer input field"), "FizzBuzz");
    await user.click(screen.getByRole("button", { name: "Submit" }));
    await screen.findByText("Answer submitted.");
    expect(screen.getByText("Next number in 12s")).toBeInTheDocument();
  });

  it("navigates to results when the session ends", async () => {
    render(<PlayPage />);
    await screen.findByText("FizzBuzz Live");

    emitSessionEnded({
      finalScores: [{ playerId: 1, playerName: "Ada", score: 30 }],
    });

    await waitFor(() =>
      expect(routerMock.push).toHaveBeenCalledWith("/results/7"),
    );
  });

  it("navigates to results when the snapshot is already finished", async () => {
    mockedGetJson.mockResolvedValue({ ...snapshot, status: "Finished" });
    render(<PlayPage />);

    await waitFor(() =>
      expect(routerMock.push).toHaveBeenCalledWith("/results/7"),
    );
    expect(signalrMocks.createConnection).not.toHaveBeenCalled();
  });

  it("shows an error when joining fails", async () => {
    mockedGetJson.mockRejectedValueOnce(new ApiError(404, "Session missing."));
    render(<PlayPage />);

    expect(await screen.findByText("Session missing.")).toBeInTheDocument();
    expect(screen.getByText("Back to games")).toBeInTheDocument();
  });
});
