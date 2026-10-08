"use client";
import { useRouter } from "next/navigation";
import React, { useState } from "react";
import { ApiError, postJson } from "@/lib/api";
import Game, { type CreateGameRequest } from "@/types/Game";
import GameRule from "@/types/GameRule";

function validateGame(
  name: string,
  startRange: number,
  endRange: number,
  rules: GameRule[],
): string | null {
  if (!name.trim()) {
    return "Name is required.";
  }
  if (name.trim().length > 100) {
    return "Name must be 100 characters or less.";
  }
  if (
    !Number.isInteger(startRange) ||
    !Number.isInteger(endRange) ||
    startRange < 1 ||
    endRange > 10000
  ) {
    return "Ranges must be whole numbers from 1 to 10000.";
  }
  if (startRange >= endRange) {
    return "End range must be greater than start range.";
  }
  if (rules.length < 1 || rules.length > 10) {
    return "Provide between 1 and 10 rules.";
  }
  for (const rule of rules) {
    if (!Number.isInteger(rule.divisibleBy) || rule.divisibleBy < 1) {
      return "Each rule divisor must be a whole number of 1 or more.";
    }
    if (!rule.replacementWord.trim()) {
      return "Each rule needs a replacement word.";
    }
    if (rule.replacementWord.trim().length > 50) {
      return "Replacement words must be 50 characters or less.";
    }
  }
  return null;
}

const CreateGamePage = () => {
  const router = useRouter();
  const [name, setName] = useState("");
  const [startRange, setStartRange] = useState(1);
  const [endRange, setEndRange] = useState(101);
  const [error, setError] = useState("");
  const [creating, setCreating] = useState(false);
  const [rules, setRules] = useState<GameRule[]>([
    { divisibleBy: 3, replacementWord: "Fizz" },
    { divisibleBy: 5, replacementWord: "Buzz" },
  ]);

  const addRule = () => {
    if (rules.length >= 10) {
      return;
    }
    setRules([...rules, { divisibleBy: 0, replacementWord: "" }]);
  };

  const deleteRule = (index: number) => {
    setRules(rules.filter((_, i) => i !== index));
  };

  const updateRule = (
    index: number,
    field: keyof GameRule,
    value: string | number,
  ) => {
    const newRules = [...rules];
    newRules[index] = {
      ...newRules[index],
      [field]: field === "divisibleBy" ? Number(value) : value,
    };
    setRules(newRules);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (creating) {
      return;
    }

    const trimmedRules = rules.map((rule) => ({
      divisibleBy: rule.divisibleBy,
      replacementWord: rule.replacementWord.trim(),
    }));
    const validationError = validateGame(
      name.trim(),
      startRange,
      endRange,
      trimmedRules,
    );
    if (validationError) {
      setError(validationError);
      return;
    }

    const body: CreateGameRequest = {
      name: name.trim(),
      startRange,
      endRange,
      rules: trimmedRules,
    };

    setCreating(true);
    try {
      await postJson<Game>("/api/games", body);
      router.push("/games");
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.detail);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Could not create the game.");
      }
      setCreating(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md mx-auto bg-white rounded-lg shadow-lg p-8">
        <h1 className="text-2xl font-bold text-center mb-8">Create New Game</h1>

        {error && (
          <div className="mb-4 p-2 bg-red-100 border border-red-400 text-red-700 rounded">
            Error: {error}
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
              maxLength={100}
              value={name}
              onChange={(e) => setName(e.target.value)}
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
                max="10000"
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
                min="1"
                max="10000"
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
                  min="1"
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
                  maxLength={50}
                  placeholder="Word"
                  value={rule.replacementWord}
                  onChange={(e) =>
                    updateRule(index, "replacementWord", e.target.value)
                  }
                  className="w-1/2 rounded-md border border-gray-300 p-2"
                />
                {rules.length > 1 && (
                  <button
                    type="button"
                    onClick={() => deleteRule(index)}
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
              disabled={rules.length >= 10}
              className="mt-2 text-sm text-blue-500 hover:text-blue-700 disabled:opacity-50"
            >
              + Add Rule
            </button>
          </div>

          <button
            type="submit"
            disabled={creating}
            className="w-full bg-blue-500 text-white py-2 px-4 rounded-md hover:bg-blue-600 transition-colors disabled:opacity-50"
          >
            {creating ? "Creating..." : "Create Game"}
          </button>
        </form>
      </div>
    </div>
  );
};

export default CreateGamePage;
