"use client";
import { ApiError, postJson } from "@/lib/api";
import Game from "@/types/Game";
import type SessionSnapshot from "@/types/Session";
import { useRouter } from "next/navigation";
import React, { useState } from "react";

interface GameModalProps {
  isOpen: boolean;
  onClose: () => void;
  game: Game;
}

const GameModal: React.FC<GameModalProps> = ({ isOpen, onClose, game }) => {
  const [durationSeconds, setDurationSeconds] = useState(60);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const router = useRouter();

  if (!isOpen) return null;

  const onPlay = async () => {
    if (creating) {
      return;
    }
    if (
      !Number.isInteger(durationSeconds) ||
      durationSeconds < 30 ||
      durationSeconds > 1800
    ) {
      setError("Duration must be a whole number from 30 to 1800 seconds.");
      return;
    }
    setError(null);
    setCreating(true);
    try {
      const session = await postJson<SessionSnapshot>("/api/sessions", {
        gameId: game.id,
        durationSeconds,
      });
      onClose();
      router.push(`/play/${session.id}`);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.detail);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Could not create the session.");
      }
      setCreating(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg p-6 sm:p-8 md:p-10 max-w-sm sm:max-w-md md:max-w-lg w-full m-4">
        <h2 className="text-xl sm:text-2xl font-bold mb-4 text-center">
          FizzBuzz Rules
        </h2>
        <div className="mb-6">
          <p className="mb-2 text-sm sm:text-base">
            The rules of FizzBuzz are simple:
          </p>
          <ul className="list-disc pl-5 text-sm sm:text-base">
            {game.rules.map((rule, index) => (
              <li key={index}>
                If the number is divisible by {rule.divisibleBy}, say{" "}
                {rule.replacementWord}
              </li>
            ))}
            <li>
              If the number divides by multiple numbers, add the word together
            </li>
            <li>
              e.g. If the number is divisible by 3 and 5, say FizzBuzz where
              Fizz is for 3 and Buzz is for 5
            </li>
            <li>Otherwise, say the number</li>
          </ul>
          <label
            htmlFor="session-duration"
            className="block text-sm font-medium text-gray-700 mt-4"
          >
            Session duration in seconds (30 to 1800)
          </label>
          <input
            id="session-duration"
            type="number"
            min={30}
            max={1800}
            value={durationSeconds}
            onChange={(e) => setDurationSeconds(Number(e.target.value))}
            className="border border-gray-300 rounded p-2 w-full mt-1"
          />
          {error && (
            <p className="mt-2 text-sm text-red-600" role="alert">
              {error}
            </p>
          )}
        </div>
        <div className="flex justify-between flex-col sm:flex-row">
          <button
            onClick={onPlay}
            disabled={creating}
            className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 mb-2 sm:mb-0 sm:mr-2 disabled:opacity-50"
          >
            {creating ? "Starting..." : "Play"}
          </button>
          <button
            onClick={onClose}
            className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default GameModal;
