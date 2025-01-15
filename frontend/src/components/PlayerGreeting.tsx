"use client";
import { useRouter } from "next/navigation";
import React, { useState } from "react";
import { BACKEND_URL } from "@/const";
const PlayerGreeting = () => {
  const [name, setName] = useState("");
  const router = useRouter();

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (name.trim() === "") {
      alert("Please enter your name");
      return;
    }
    const response = await fetch(`${BACKEND_URL}/Players/login`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ name: name }),
    });
    const data = await response.json();
    localStorage.setItem("playerName", name);
    localStorage.setItem("player", JSON.stringify(data));
    router.push("/games");
  };

  return (
    <div>
      <div className="flex items-center justify-center">
        <form
          onSubmit={handleSubmit}
          className="bg-white p-6 rounded shadow-lg"
        >
          <h2 className="text-xl mb-4">Welcome to the Game!</h2>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Enter your name"
            className="border-2 py-2 px-4 mb-4 rounded-md w-full"
            required
          />
          <button
            type="submit"
            className="bg-blue-500 text-white py-2 px-4 rounded w-full"
          >
            Submit
          </button>
        </form>
      </div>
    </div>
  );
};

export default PlayerGreeting;
