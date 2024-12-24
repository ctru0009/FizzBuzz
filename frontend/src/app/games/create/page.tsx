"use client";
import { useRouter } from "next/navigation";
import React, { useState } from "react";
import { BACKEND_URL } from "@/const";
import GameRule from "@/types/GameRule";

const CreateGamePage = () => {
  const router = useRouter();
  const [name, setName] = useState("");
  const [authorName, setAuthorName] = useState("");
  const [startRange, setStartRange] = useState(1);
  const [endRange, setEndRange] = useState(101);
  const [error, setError] = useState("");
  const [rules, setRules] = useState<GameRule[]>([
    { divisibleBy: 3, replacementWord: "Fizz" },
    { divisibleBy: 5, replacementWord: "Buzz" },
  ]);
  const player = localStorage.getItem("player");
  const playerId = player ? JSON.parse(player).id : null;

  const addRule = () => {
    setRules([...rules, { divisibleBy: 0, replacementWord: "" }]);
  };

  const removeRule = (index: number) => {
    if (rules.length <= 2) {
      setError("Minimum 2 rules required");
      return;
    }
    setRules(rules.filter((_, i) => i !== index));
    setError("");
  };

  const updateRule = (
    index: number,
    field: keyof GameRule,
    value: string | number
  ) => {
    const newRules = [...rules];
    newRules[index] = {
      ...newRules[index],
      [field]: field === "divisibleBy" ? Number(value) : value,
    };
    setRules(newRules);
  };

  const validateForm = (): boolean => {
    if (rules.length < 2) {
      setError("Minimum 2 rules required");
      return false;
    }
    if (startRange < 1) {
      setError("Start range must be greater than 1");
      return false;
    }
    if (endRange - startRange < 100) {
      setError("Range must be at least 100 numbers");
      return false;
    }
    setError("");
    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    try {
      const response = await fetch(`${BACKEND_URL}/games`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name,
          playerId: playerId,
          authorName,
          rules,
          startRange,
          endRange,
        }),
      });

      if (response.ok) {
        router.push("/games");
      } else {
        setError("Failed to create game");
      }
    } catch (error) {
      setError("Error creating game");
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md mx-auto bg-white rounded-lg shadow-lg p-8">
        <h1 className="text-2xl font-bold text-center mb-8">Create New Game</h1>

        {error && (
          <div className="mb-4 p-2 bg-red-100 border border-red-400 text-red-700 rounded">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700">
              Game Name
            </label>
            <input
              type="text"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="mt-1 block w-full rounded-md border border-gray-300 p-2"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">
              Author Name
            </label>
            <input
              type="text"
              required
              value={authorName}
              onChange={(e) => setAuthorName(e.target.value)}
              className="mt-1 block w-full rounded-md border border-gray-300 p-2"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700">
                Start Range
              </label>
              <input
                type="number"
                required
                min="1"
                value={startRange}
                onChange={(e) => {
                  setStartRange(Number(e.target.value));
                  setError("");
                }}
                className="mt-1 block w-full rounded-md border border-gray-300 p-2"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">
                End Range
              </label>
              <input
                type="number"
                required
                min={startRange + 100}
                value={endRange}
                onChange={(e) => {
                  setEndRange(Number(e.target.value));
                  setError("");
                }}
                className="mt-1 block w-full rounded-md border border-gray-300 p-2"
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Game Rules
            </label>
            {rules.map((rule, index) => (
              <div key={index} className="flex gap-2 mb-2">
                <input
                  type="number"
                  required
                  placeholder="Divisible by"
                  value={rule.divisibleBy}
                  onChange={(e) =>
                    updateRule(index, "divisibleBy", e.target.value)
                  }
                  className="w-1/3 rounded-md border border-gray-300 p-2"
                />
                <input
                  type="text"
                  required
                  placeholder="Word"
                  value={rule.replacementWord}
                  onChange={(e) =>
                    updateRule(index, "replacementWord", e.target.value)
                  }
                  className="w-1/2 rounded-md border border-gray-300 p-2"
                />
                {rules.length > 2 && (
                  <button
                    type="button"
                    onClick={() => removeRule(index)}
                    className="text-red-500 hover:text-red-700"
                  >
                    ✕
                  </button>
                )}
              </div>
            ))}
            <button
              type="button"
              onClick={addRule}
              className="mt-2 text-sm text-blue-500 hover:text-blue-700"
            >
              + Add Rule
            </button>
          </div>

          <button
            type="submit"
            className="w-full bg-blue-500 text-white py-2 px-4 rounded-md hover:bg-blue-600 transition-colors"
          >
            Create Game
          </button>
        </form>
      </div>
    </div>
  );
};

export default CreateGamePage;
