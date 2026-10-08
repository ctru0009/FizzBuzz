"use client";
import { ApiError, getJson } from "@/lib/api";
import type { SessionSnapshot } from "@/types/Session";
import { useParams } from "next/navigation";
import React, { useEffect, useState } from "react";

const ResultsPage = () => {
  const params = useParams();
  const rawId = params.id;
  const sessionIdParam = Array.isArray(rawId) ? rawId[0] : rawId;
  const sessionId =
    sessionIdParam !== undefined && /^\d+$/.test(sessionIdParam)
      ? Number(sessionIdParam)
      : NaN;
  const [session, setSession] = useState<SessionSnapshot | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (Number.isNaN(sessionId)) {
      setError("Invalid session id.");
      setLoading(false);
      return;
    }
    let cancelled = false;
    const fetchSession = async () => {
      try {
        const data = await getJson<SessionSnapshot>(
          `/api/sessions/${sessionId}`,
        );
        if (!cancelled) {
          setSession(data);
          setLoading(false);
        }
      } catch (err) {
        if (!cancelled) {
          setLoading(false);
          if (err instanceof ApiError) {
            setError(err.detail);
          } else if (err instanceof Error) {
            setError(err.message);
          } else {
            setError("Could not load the session.");
          }
        }
      }
    };
    fetchSession();
    return () => {
      cancelled = true;
    };
  }, [sessionId]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  if (error || !session) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen gap-4">
        <div className="text-xl text-red-500">
          {error ?? "Session not found"}
        </div>
        <a
          href="/games"
          className="bg-blue-500 text-white px-6 py-2 rounded-lg hover:bg-blue-600 transition-colors"
        >
          Back to games
        </a>
      </div>
    );
  }

  const finalScores = [...session.scores].sort((a, b) => b.score - a.score);

  return (
    <div className="min-h-screen bg-gray-100 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-3xl mx-auto">
        <div className="bg-white shadow-lg rounded-lg overflow-hidden">
          <div className="px-6 py-8">
            <h1 className="text-3xl font-bold text-center text-gray-900 mb-8">
              Game Results
            </h1>

            <div className="grid grid-cols-1 gap-6 mb-8">
              <div className="bg-blue-50 p-6 rounded-lg">
                <h2 className="text-2xl font-bold text-center text-blue-600 mb-6">
                  Final Scores
                </h2>

                {finalScores.length === 0 ? (
                  <p className="text-center text-gray-500">No scores yet.</p>
                ) : (
                  <table className="w-full text-left border-collapse bg-white rounded-lg shadow">
                    <thead>
                      <tr className="border-b">
                        <th className="py-2 px-4">Rank</th>
                        <th className="py-2 px-4">Player</th>
                        <th className="py-2 px-4 text-right">Score</th>
                      </tr>
                    </thead>
                    <tbody>
                      {finalScores.map((entry, index) => (
                        <tr key={entry.playerId} className="border-b">
                          <td className="py-2 px-4">{index + 1}</td>
                          <td className="py-2 px-4">{entry.playerName}</td>
                          <td className="py-2 px-4 text-right font-semibold">
                            {entry.score}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}

                <div className="grid grid-cols-2 gap-4 text-gray-700 mt-6">
                  <div className="flex flex-col items-center p-4 bg-white rounded-lg shadow">
                    <span className="text-sm font-medium">Start Time</span>
                    <span className="text-lg">
                      {new Date(session.startTimeUtc).toLocaleString()}
                    </span>
                  </div>

                  <div className="flex flex-col items-center p-4 bg-white rounded-lg shadow">
                    <span className="text-sm font-medium">End Time</span>
                    <span className="text-lg">
                      {new Date(session.endTimeUtc).toLocaleString()}
                    </span>
                  </div>

                  <div className="flex flex-col items-center p-4 bg-white rounded-lg shadow">
                    <span className="text-sm font-medium">Status</span>
                    <span className="text-lg">{session.status}</span>
                  </div>

                  <div className="flex flex-col items-center p-4 bg-white rounded-lg shadow">
                    <span className="text-sm font-medium">Game ID</span>
                    <span className="text-lg">{session.gameId}</span>
                  </div>
                </div>
              </div>
            </div>

            <div className="flex justify-center">
              <a
                href="/games"
                className="bg-blue-500 text-white px-6 py-2 rounded-lg hover:bg-blue-600 transition-colors"
              >
                Play Again
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ResultsPage;
