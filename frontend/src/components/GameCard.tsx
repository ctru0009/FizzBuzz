"use client";
import React, { useState } from "react";
import GameModal from "./GameModal";
import Game from "@/types/Game";

interface GameCardProps {
  game: Game;
}

const GameCard: React.FC<GameCardProps> = ({
  game,
}) => {
  const [isModalOpen, setIsModalOpen] = useState(false);

  return (
    <>
      <div className="flex flex-col bg-white border border-sky-500 shadow-lg rounded-lg p-6 my-4">
        <h2 className="text-2xl font-bold mb-2 text-center">{game.name}</h2>
        <p className="text-gray-700 mb-4 text-center">{game.authorName}</p>
        <p className="text-gray-700 mb-4 text-center">{new Date(game.createdAt).toUTCString()}</p>
        <button
          className="mt-auto bg-blue-500 text-white py-2 px-4 rounded hover:bg-blue-600"
          onClick={() => setIsModalOpen(true)}
        >
          Play
        </button>
      </div>
      <GameModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        game={game}
      ></GameModal>
    </>
  );
};

export default GameCard;
