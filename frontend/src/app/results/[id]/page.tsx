"use client";
import { BACKEND_URL } from "@/const";
import SessionResponse from "@/types/Session";
import { useParams } from "next/navigation";
import React, { useEffect, useState } from "react";

const ResultsPage = () => {
  const params = useParams();
  const id = params.id;
  const [session, setSession] = useState<SessionResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchSession = async () => {
      try {
        const response = await fetch(`${BACKEND_URL}/sessions/${id}`);
        const data = await response.json();
        setSession(data);
        setLoading(false);
      } catch (error) {
        console.error("Error fetching session:", error);
        setLoading(false);
      }
    };
    fetchSession();
  }, [id]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  if (!session) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-xl text-red-500">Session not found</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-100 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-3xl mx-auto">
        <div className="bg-white shadow-lg rounded-lg overflow-hidden">
          <div className="px-6 py-8">
            <h1 className="text-3xl font-bold text-center text-gray-900 mb-8">
              Game Results
            </h1>
            
            <div className="grid grid-cols-1 gap-6 mb-8">
              <div className="bg-blue-50 p-6 rounded-lg">
                <h2 className="text-2xl font-bold text-center text-blue-600 mb-6">
                  Final Score: {session.score}
                </h2>
                
                <div className="grid grid-cols-2 gap-4 text-gray-700">
                  <div className="flex flex-col items-center p-4 bg-white rounded-lg shadow">
                    <span className="text-sm font-medium">Start Time</span>
                    <span className="text-lg">
                      {new Date(session.startTime).toLocaleString()}
                    </span>
                  </div>
                  
                  <div className="flex flex-col items-center p-4 bg-white rounded-lg shadow">
                    <span className="text-sm font-medium">End Time</span>
                    <span className="text-lg">
                      {new Date(session.endTime).toLocaleString()}
                    </span>
                  </div>
                  
                  <div className="flex flex-col items-center p-4 bg-white rounded-lg shadow">
                    <span className="text-sm font-medium">Duration</span>
                    <span className="text-lg">{session.duration} seconds</span>
                  </div>
                  
                  <div className="flex flex-col items-center p-4 bg-white rounded-lg shadow">
                    <span className="text-sm font-medium">Game ID</span>
                    <span className="text-lg">{session.gameId}</span>
                  </div>
                </div>
              </div>
            </div>

            <div className="flex justify-center">
              <a
                href="/games"
                className="bg-blue-500 text-white px-6 py-2 rounded-lg hover:bg-blue-600 transition-colors"
              >
                Play Again
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ResultsPage;