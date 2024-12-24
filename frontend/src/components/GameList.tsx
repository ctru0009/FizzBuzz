"use client";
import React, { useEffect, useState } from "react";
import GameCard from "./GameCard";
import Game from "@/types/Game";
import { BACKEND_URL } from "@/const";
import { redirect, useRouter } from "next/navigation";

type SortOption = "name" | "createdAt";

const GameList = () => {
  const [sortOrder, setSortOrder] = useState<SortOption>("name");
  const [games, setGames] = useState<Game[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();

  useEffect(() => {
    const fetchGames = async () => {
      try {
        setIsLoading(true);
        setError(null);
        const response = await fetch(`${BACKEND_URL}/games`);
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        const data = await response.json();
        setGames(data);
      } catch (error) {
        setError(
          error instanceof Error ? error.message : "Failed to fetch games"
        );
      } finally {
        setIsLoading(false);
      }
    };
    fetchGames();
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
      <div className="p-4 flex items-center justify-between">
        <button className="bg-blue-500 text-white py-2 px-4 rounded hover:bg-blue-600 w-auto" onClick={handleClick}>
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
