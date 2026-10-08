import { SIGNALR_URL } from "@/const";
import type {
  JoinSessionResult,
  NumberAdvancedPayload,
  ScoresUpdatedPayload,
  SessionEndedPayload,
} from "@/types/Session";
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
} from "@microsoft/signalr";

export type Unsubscribe = () => void;

function createConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(SIGNALR_URL, { withCredentials: true })
    .withAutomaticReconnect()
    .build();
}

function onNumberAdvanced(
  connection: HubConnection,
  handler: (_payload: NumberAdvancedPayload) => void,
): Unsubscribe {
  connection.on("NumberAdvanced", handler);
  return () => connection.off("NumberAdvanced", handler);
}

function onScoresUpdated(
  connection: HubConnection,
  handler: (_payload: ScoresUpdatedPayload) => void,
): Unsubscribe {
  connection.on("ScoresUpdated", handler);
  return () => connection.off("ScoresUpdated", handler);
}

function onSessionEnded(
  connection: HubConnection,
  handler: (_payload: SessionEndedPayload) => void,
): Unsubscribe {
  connection.on("SessionEnded", handler);
  return () => connection.off("SessionEnded", handler);
}

function onReconnectedResume(
  connection: HubConnection,
  sessionId: number,
  handler: (_state: JoinSessionResult) => void,
): Unsubscribe {
  const reconnected = async () => {
    const state = await connection.invoke<JoinSessionResult>(
      "JoinSession",
      sessionId,
    );
    handler(state);
  };
  connection.onreconnected(reconnected);
  return () => connection.onreconnected(() => {});
}

async function stopConnection(connection: HubConnection): Promise<void> {
  connection.off("NumberAdvanced");
  connection.off("ScoresUpdated");
  connection.off("SessionEnded");
  connection.onreconnected(() => {});
  if (connection.state !== HubConnectionState.Disconnected) {
    await connection.stop();
  }
}

export {
  createConnection,
  onNumberAdvanced,
  onReconnectedResume,
  onScoresUpdated,
  onSessionEnded,
  stopConnection,
};
export type { JoinSessionResult };
