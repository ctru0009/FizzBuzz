"use client";
import React, { useEffect, useState } from "react";
import GameCard from "./GameCard";
import Game from "@/types/Game";
import type { OpenSessionEntry } from "@/types/Session";
import { getJson } from "@/lib/api";
import { useRouter } from "next/navigation";

type SortOption = "name" | "createdAt";

const GameList = () => {
  const [sortOrder, setSortOrder] = useState<SortOption>("name");
  const [games, setGames] = useState<Game[]>([]);
  const [openSessions, setOpenSessions] = useState<OpenSessionEntry[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [openError, setOpenError] = useState<string | null>(null);
  const router = useRouter();

  useEffect(() => {
    let cancelled = false;
    const fetchData = async () => {
      setIsLoading(true);
      setError(null);
      setOpenError(null);
      try {
        const data = await getJson<Game[]>("/games");
        if (!cancelled) {
          setGames(data);
        }
      } catch (fetchError) {
        if (!cancelled) {
          setError(
            fetchError instanceof Error
              ? fetchError.message
              : "Failed to fetch games",
          );
        }
      }
      try {
        const sessions = await getJson<OpenSessionEntry[]>("/sessions/open");
        if (!cancelled) {
          setOpenSessions(sessions);
        }
      } catch (fetchError) {
        if (!cancelled) {
          setOpenError(
            fetchError instanceof Error
              ? fetchError.message
              : "Failed to fetch open sessions",
          );
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    };
    fetchData();
    return () => {
      cancelled = true;
    };
  }, []);

  const sortedGames = [...games].sort((a, b) => {
    if (sortOrder === "name") {
      return a.name.localeCompare(b.name);
    }
    if (sortOrder === "createdAt") {
      return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
    }
    return 0;
  });

  const handleClick = async () => {
    router.push("/games/create");
  };

  if (isLoading) return <div className="p-4">Loading games...</div>;
  if (error) return <div className="p-4 text-red-500">Error: {error}</div>;

  return (
    <div>
      <div className="p-4">
        <h2 className="text-xl font-bold mb-2">Open sessions</h2>
        {openError && <p className="text-red-500 mb-2">Error: {openError}</p>}
        {!openError && openSessions.length === 0 && (
          <p className="text-gray-500 mb-2">
            No open sessions right now. Start one from a game below.
          </p>
        )}
        {!openError && openSessions.length > 0 && (
          <ul className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-2">
            {openSessions.map((session) => (
              <li
                key={session.id}
                className="bg-white p-4 rounded shadow flex flex-col gap-2"
              >
                <p className="font-bold">{session.gameName}</p>
                <p className="text-sm text-gray-600">
                  {session.playerCount} player
                  {session.playerCount === 1 ? "" : "s"} playing
                </p>
                <p className="text-sm text-gray-600">
                  Ends at: {new Date(session.endsAtUtc).toLocaleString()}
                </p>
                <button
                  className="bg-green-500 text-white py-2 px-4 rounded hover:bg-green-600 w-auto"
                  onClick={() => router.push(`/play/${session.id}`)}
                >
                  Join
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
      <div className="p-4 flex items-center justify-between">
        <button
          className="bg-blue-500 text-white py-2 px-4 rounded hover:bg-blue-600 w-auto"
          onClick={handleClick}
        >
          Create New Game
        </button>
        <div className="flex items-center">
          <label htmlFor="sort" className="mr-2">
            Sort by:
          </label>
          <select
            id="sort"
            value={sortOrder}
            onChange={(e) => setSortOrder(e.target.value as SortOption)}
            className="border rounded p-1"
          >
            <option value="name">Name</option>
            <option value="createdAt">Date Created</option>
          </select>
        </div>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 p-4">
        {sortedGames.map((game) => (
          <GameCard key={game.id} game={game} />
        ))}
      </div>
    </div>
  );
};

export default GameList;
