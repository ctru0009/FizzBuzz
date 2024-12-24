"use client";
import { BACKEND_URL, SIGNALR_URL } from "@/const";
import SessionResponse from "@/types/Session";
import { redirect, useParams, useRouter } from "next/navigation";
import * as signalR from "@microsoft/signalr";
import React, { useEffect, useState } from "react";
import GameAnswerResponse from "@/types/GameAnswerResponse";
import GameAnswerSubmit from "@/types/GameAnswerSubmit";

const Page = () => {
  const params = useParams();
  const id = params.id;
  const [session, setSession] = useState<SessionResponse | null>(null);
  const [gameUpdate, setGameUpdate] = useState<GameAnswerResponse | null>(null);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(
    null
  );
  const [answer, setAnswer] = useState<string>("");
  const [score, setScore] = useState<number>(0);
  const [nextNumber, setNextNumber] = useState<number>(0);
  const [gameId, setGameId] = useState<number>(0);
  const [loading, setLoading] = useState<boolean>(true);
  const [isGameOver, setIsGameOver] = useState(false);
  const router = useRouter();
  useEffect(() => {
    const fetchSession = async () => {
      try {
        const response = await fetch(`${BACKEND_URL}/sessions/${id}`);
        const data: SessionResponse = await response.json();
        setSession(data);
        setGameId(Number(data.gameId));
        setLoading(false);

        const newConnection = new signalR.HubConnectionBuilder()
          .withUrl(`${SIGNALR_URL}`)
          .withAutomaticReconnect()
          .build();

        await newConnection.start();
        setConnection(newConnection);
        // Listen for updates to the game session
      } catch (error) {
        console.error("Error fetching session:", error);
      }
    };
    fetchSession();
  }, [id]);

  useEffect(() => {
    if (!connection || !session || loading) return;

    const gameAnswerSubmit: GameAnswerSubmit = {
      sessionId: Number(id),
      gameId: Number(gameId),
      number: 0,
      startRange: session?.startRange,
      endRange: session?.endRange,
      answer: "",
      score: 0,
    };
    connection
      .invoke("JoinGameSession", gameAnswerSubmit)
      .then(() => {
        console.log("Joined game session");
      })
      .catch((error) => console.error(error));
    connection.on("ReceiveGameUpdate", (result: GameAnswerResponse) => {
      setGameUpdate(result);
      setScore(score + result.score);
      setNextNumber(result.nextNumber);
      console.log(result);
      console.log("gameId: " + gameId);
    });
  }, [connection]);

  useEffect(() => {
    if (session) {
      const interval = setInterval(async () => {
        setSession((prev) => {
          if (prev && prev.duration > 0) {
            return { ...prev, duration: prev.duration - 1 };
          }
          if (prev && prev.duration === 0 && !isGameOver) {
            setIsGameOver(true);
            // Save final score
          }
          return prev;
        });
      }, 1000);
      return () => clearInterval(interval);
    }
  }, [session]);

  useEffect(() => {
    if (!isGameOver) return;
    fetch(`${BACKEND_URL}/sessions/${id}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        sessionId: Number(id),
        score: score,
      }),
    })
      .then(() => {
        console.log("Final score saved");
        router.push(`/results/${id}`);
      })
      .catch((error) => console.error("Error saving final score:", error));
  }, [isGameOver]);

  const handleSubmit = async () => {
    if (!connection) return;
    await connection
      .invoke("SubmitAnswer", {
        sessionId: Number(id),
        gameId: Number(gameId),
        number: gameUpdate?.nextNumber,
        startRange: session?.startRange,
        endRange: session?.endRange,
        answer,
        score: gameUpdate?.score,
      })
      .catch((error) => console.error(error));
    setAnswer("");
  };

  return (
    <div className="flex flex-col items-center p-6 bg-gray-100 min-h-screen">
      {session ? (
        <div className="w-full max-w-2xl bg-white shadow-lg rounded-lg p-8">
          <h1 className="text-2xl font-bold mb-4 text-center text-blue-600">
            Welcome to the FizzBuzz Game!
          </h1>
          <div className="game-board w-full h-64 bg-gray-200 border border-gray-300 rounded mb-6 flex items-center justify-center">
            {/* Game board elements */}
            <p className="text-black text-lg">{nextNumber}</p>
          </div>
          <div className="game-controls flex justify-center space-x-4 mb-6">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                handleSubmit();
              }}
              className="flex items-center space-x-4"
            >
              <div className="flex flex-col gap-2">
                <label
                  htmlFor="answer-input"
                  className="text-sm font-medium text-gray-700"
                >
                  Your Answer
                </label>
                <input
                  id="answer-input"
                  type="text"
                  className="border border-gray-300 rounded p-2 w-1/2"
                  placeholder="Enter your answer"
                  value={answer}
                  onChange={(e) => setAnswer(e.target.value)}
                  aria-label="Answer input field"
                  aria-required="true"
                  required
                />
              </div>
              <button
                type="submit"
                className="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600"
              >
                Submit
              </button>
            </form>
          </div>
          <div className="game-status text-center">
            <p className="text-lg">
              Score: <span className="font-semibold">{score}</span>
            </p>
            <p className="text-lg">
              Time: <span className="font-semibold">{session.duration}</span>
            </p>
          </div>
        </div>
      ) : (
        <p className="text-xl text-gray-600">Loading...</p>
      )}
    </div>
  );
};

export default Page;
