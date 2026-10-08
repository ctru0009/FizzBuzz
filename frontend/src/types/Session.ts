interface SessionScore {
  playerId: number;
  playerName: string;
  score: number;
}

interface SessionSnapshot {
  id: number;
  gameId: number;
  status: "Open" | "Finished";
  startTimeUtc: string;
  endTimeUtc: string;
  currentNumber: number;
  currentRound: number;
  scores: SessionScore[];
}

interface OpenSessionEntry {
  id: number;
  gameId: number;
  gameName: string;
  playerCount: number;
  endsAtUtc: string;
}

interface CreateSessionRequest {
  gameId: number;
  durationSeconds: number;
}

interface JoinSessionResult {
  sessionId: number;
  number: number;
  round: number;
  endsAtUtc: string;
  scores: SessionScore[];
  answeredCurrentRound: boolean;
}

interface NumberAdvancedPayload {
  number: number;
  round: number;
  endsAtUtc: string;
  advancesAtUtc?: string;
}

interface ScoresUpdatedPayload {
  scores: SessionScore[];
}

interface SessionEndedPayload {
  finalScores: SessionScore[];
}

interface SubmitAnswerResult {
  correct: boolean;
  score: number;
}

export default SessionSnapshot;
export type {
  CreateSessionRequest,
  JoinSessionResult,
  NumberAdvancedPayload,
  OpenSessionEntry,
  ScoresUpdatedPayload,
  SessionEndedPayload,
  SessionScore,
  SessionSnapshot,
  SubmitAnswerResult,
};
