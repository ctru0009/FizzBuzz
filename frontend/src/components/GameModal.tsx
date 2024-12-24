"use client";
import { BACKEND_URL } from "@/const";
import Game from "@/types/Game";
import GameRule from "@/types/GameRule";
import { redirect, useRouter } from "next/navigation";
import React, { useState } from "react";

interface GameModalProps {
  isOpen: boolean;
  onClose: () => void;
  game: Game;
}

const GameModal: React.FC<GameModalProps> = ({ isOpen, onClose, game }) => {
  const [duration, setDuration] = useState(60);
  if (!isOpen) return null;
  const router = useRouter();
  const onPlay = () => {
    onClose();
    const gameId = game.id;
    const player = localStorage.getItem("player");
    if (!player) {
      router.push("/");
      return;
    }
    const playerId = JSON.parse(player).id;

    // Create a new game session
    fetch(`${BACKEND_URL}/Sessions`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        gameId,
        playerId,
        duration: duration,
      }),
    })
      .then((response) => response.json())
      .then((data) => {
        router.push(`/play/${data.id}`);
      });
    
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
          <input
            type="number"
            value={duration}
            onChange={(e) => setDuration(parseInt(e.target.value))}
            className="border border-gray-300 rounded p-2 w-full mt-4"
          />
        </div>
        <div className="flex justify-between flex-col sm:flex-row">
          <button
            onClick={onPlay}
            className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 mb-2 sm:mb-0 sm:mr-2"
          >
            Play
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
