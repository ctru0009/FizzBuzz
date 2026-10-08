import type GameRule from "./GameRule";

interface Game {
  id: number;
  name: string;
  authorName: string;
  startRange: number;
  endRange: number;
  createdAt: string;
  rules: GameRule[];
}

interface CreateGameRequest {
  name: string;
  startRange: number;
  endRange: number;
  rules: GameRule[];
}

export default Game;
export type { CreateGameRequest };
