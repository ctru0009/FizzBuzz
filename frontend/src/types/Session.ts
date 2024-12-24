interface SessionResponse {
    id: string;
    gameId: string;
    playerId: string;
    duration: number;
    startRange: number;
    endRange: number;
    startTime: string;
    endTime: string;
    score: number;
    }

export default SessionResponse;