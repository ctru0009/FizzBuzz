"use client";
import { ApiError, getJson } from "@/lib/api";
import {
  createConnection,
  onNumberAdvanced,
  onReconnectedResume,
  onScoresUpdated,
  onSessionEnded,
  stopConnection,
  type Unsubscribe,
} from "@/lib/signalr";
import type {
  JoinSessionResult,
  SessionScore,
  SessionSnapshot,
  SubmitAnswerResult,
} from "@/types/Session";
import type { HubConnection } from "@microsoft/signalr";
import { useParams, useRouter } from "next/navigation";
import React, { useEffect, useRef, useState } from "react";

function sortScores(scores: SessionScore[]): SessionScore[] {
  return [...scores].sort((a, b) => b.score - a.score);
}

function countdownText(advancesAtUtc: string | null, now: number): string {
  if (!advancesAtUtc) {
    return "Waiting for the next number...";
  }
  const target = new Date(advancesAtUtc).getTime();
  if (Number.isNaN(target)) {
    return "Waiting for the next number...";
  }
  const remaining = Math.max(0, Math.ceil((target - now) / 1000));
  return `Next number in ${remaining}s`;
}

const PlayPage = () => {
  const params = useParams();
  const router = useRouter();
  const rawId = params.id;
  const sessionIdParam = Array.isArray(rawId) ? rawId[0] : rawId;
  const sessionId =
    sessionIdParam !== undefined && /^\d+$/.test(sessionIdParam)
      ? Number(sessionIdParam)
      : NaN;

  const [currentNumber, setCurrentNumber] = useState<number | null>(null);
  const [currentRound, setCurrentRound] = useState<number | null>(null);
  const [scores, setScores] = useState<SessionScore[]>([]);
  const [answered, setAnswered] = useState(false);
  const [advancesAtUtc, setAdvancesAtUtc] = useState<string | null>(null);
  const [answer, setAnswer] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [now, setNow] = useState(() => Date.now());
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 250);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    if (Number.isNaN(sessionId)) {
      setError("Invalid session id.");
      setLoading(false);
      return;
    }

    let cancelled = false;
    let connection: HubConnection | null = null;
    const cleanups: Unsubscribe[] = [];

    const applyJoinState = (state: JoinSessionResult) => {
      if (cancelled) {
        return;
      }
      setCurrentNumber(state.number);
      setCurrentRound(state.round);
      setScores(sortScores(state.scores));
      setAnswered(state.answeredCurrentRound);
      setLoading(false);
    };

    const init = async () => {
      try {
        const snapshot = await getJson<SessionSnapshot>(
          `/api/sessions/${sessionId}`,
        );
        if (cancelled) {
          return;
        }
        if (snapshot.status === "Finished") {
          router.push(`/results/${sessionId}`);
          return;
        }
        setCurrentNumber(snapshot.currentNumber);
        setCurrentRound(snapshot.currentRound);
        setScores(sortScores(snapshot.scores));

        const conn = createConnection();
        connection = conn;
        connectionRef.current = conn;
        cleanups.push(
          onNumberAdvanced(conn, (payload) => {
            setCurrentNumber(payload.number);
            setCurrentRound(payload.round);
            setAdvancesAtUtc(payload.advancesAtUtc ?? null);
            setAnswered(false);
            setFeedback(null);
          }),
          onScoresUpdated(conn, (payload) => {
            setScores(sortScores(payload.scores));
          }),
          onSessionEnded(conn, () => {
            router.push(`/results/${sessionId}`);
          }),
        );
        await conn.start();
        if (cancelled) {
          await stopConnection(conn);
          return;
        }
        cleanups.push(onReconnectedResume(conn, sessionId, applyJoinState));
        const state = await conn.invoke<JoinSessionResult>(
          "JoinSession",
          sessionId,
        );
        applyJoinState(state);
      } catch (err) {
        if (cancelled) {
          return;
        }
        setLoading(false);
        if (err instanceof ApiError) {
          setError(err.detail);
        } else if (err instanceof Error) {
          setError(err.message);
        } else {
          setError("Could not join the session.");
        }
      }
    };

    init();

    return () => {
      cancelled = true;
      for (const cleanup of cleanups) {
        cleanup();
      }
      connectionRef.current = null;
      if (connection) {
        void stopConnection(connection);
      }
    };
  }, [sessionId, router]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const conn = connectionRef.current;
    if (!conn || currentRound === null || submitting) {
      return;
    }
    const trimmed = answer.trim();
    if (!trimmed) {
      setFeedback("Answer is required.");
      return;
    }
    if (trimmed.length > 100) {
      setFeedback("Answer must be 100 characters or less.");
      return;
    }
    setSubmitting(true);
    try {
      const result = await conn.invoke<SubmitAnswerResult>(
        "SubmitAnswer",
        sessionId,
        currentRound,
        trimmed,
      );
      setAnswered(true);
      setFeedback(
        result.correct
          ? `Correct! Your score is ${result.score}.`
          : "Wrong answer. Wait for the next number.",
      );
      setAnswer("");
    } catch (err) {
      setFeedback(
        err instanceof Error ? err.message : "Could not submit the answer.",
      );
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen gap-4">
        <div className="text-xl text-red-500">{error}</div>
        <a
          href="/games"
          className="bg-blue-500 text-white px-6 py-2 rounded-lg hover:bg-blue-600 transition-colors"
        >
          Back to games
        </a>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center p-6 bg-gray-100 min-h-screen">
      <div className="w-full max-w-2xl bg-white shadow-lg rounded-lg p-8">
        <h1 className="text-2xl font-bold mb-4 text-center text-blue-600">
          FizzBuzz Live
        </h1>
        <p className="text-center text-gray-600 mb-4">
          Round <span className="font-semibold">{currentRound}</span>
        </p>
        <div className="game-board w-full h-64 bg-gray-200 border border-gray-300 rounded mb-6 flex items-center justify-center">
          <p className="text-black text-6xl font-bold">{currentNumber}</p>
        </div>

        {answered ? (
          <div className="text-center mb-6">
            <p className="text-lg font-semibold text-green-600">
              Answer submitted.
            </p>
            <p className="text-gray-600">{countdownText(advancesAtUtc, now)}</p>
          </div>
        ) : (
          <div className="game-controls flex justify-center mb-6">
            <form onSubmit={handleSubmit} className="flex items-end space-x-4">
              <div className="flex flex-col gap-2">
                <label
                  htmlFor="answer-input"
                  className="text-sm font-medium text-gray-700"
                >
                  Your Answer
                </label>
                <input
                  id="answer-input"
                  type="text"
                  className="border border-gray-300 rounded p-2"
                  placeholder="Enter your answer"
                  value={answer}
                  onChange={(e) => setAnswer(e.target.value)}
                  aria-label="Answer input field"
                  aria-required="true"
                  required
                />
              </div>
              <button
                type="submit"
                disabled={submitting}
                className="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600 disabled:opacity-50"
              >
                Submit
              </button>
            </form>
          </div>
        )}

        {feedback && (
          <p className="text-center text-gray-700 mb-4">{feedback}</p>
        )}

        <div className="game-status">
          <h2 className="text-xl font-bold text-center mb-2">Leaderboard</h2>
          {scores.length === 0 ? (
            <p className="text-center text-gray-500">No scores yet.</p>
          ) : (
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b">
                  <th className="py-2 pr-4">Rank</th>
                  <th className="py-2 pr-4">Player</th>
                  <th className="py-2 text-right">Score</th>
                </tr>
              </thead>
              <tbody>
                {scores.map((entry, index) => (
                  <tr key={entry.playerId} className="border-b">
                    <td className="py-2 pr-4">{index + 1}</td>
                    <td className="py-2 pr-4">{entry.playerName}</td>
                    <td className="py-2 text-right font-semibold">
                      {entry.score}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
};

export default PlayPage;
