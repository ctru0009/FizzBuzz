import GameRule from "./GameRule";

interface Game {
  id: string;
  name: string;
  authorName: string;
  rules: GameRule[];
  startRange: number;
  endRange: number;
  createdAt: string;
}

export default Game;
