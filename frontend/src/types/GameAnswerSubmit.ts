// public class GameAnswerSubmit
// {
//     public int SessionId { get; set; }
//     public int Number { get; set; }
//     public int StartRange { get; set; }
//     public int EndRange { get; set; }
//     public required string Answer { get; set; }
//     public int Score { get; set; }
// }

interface GameAnswerSubmit {
  sessionId: number;
  gameId: number;
  number: number;
  startRange: number;
  endRange: number;
  answer: string;
  score: number;
}

export default GameAnswerSubmit;
