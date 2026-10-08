"use client";

import { Suspense, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { ApiError } from "@/lib/api";
import { AuthProvider, useAuth } from "@/lib/auth";

type Mode = "login" | "register";

function AuthForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { player, loading, login, register } = useAuth();
  const [mode, setMode] = useState<Mode>("login");
  const [name, setName] = useState("");
  const [password, setPassword] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const requested = searchParams.get("returnUrl");
  const returnUrl =
    requested && requested.startsWith("/") && !requested.startsWith("//")
      ? requested
      : "/games";

  useEffect(() => {
    if (!loading && player) {
      router.replace(returnUrl);
    }
  }, [loading, player, returnUrl, router]);

  if (loading) {
    return (
      <div className="flex items-center justify-center">
        <p className="bg-white p-6 rounded shadow-lg">Checking sign in...</p>
      </div>
    );
  }

  if (player) {
    return null;
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    const trimmedName = name.trim();
    if (trimmedName.length < 1 || trimmedName.length > 50) {
      setFormError("Name is required and must be 1 to 50 characters.");
      return;
    }
    if (password.length < 8 || password.length > 100) {
      setFormError("Password is required and must be 8 to 100 characters.");
      return;
    }
    setFormError(null);
    setSubmitting(true);
    try {
      if (mode === "login") {
        await login(trimmedName, password);
      } else {
        await register(trimmedName, password);
      }
      router.replace(returnUrl);
    } catch (error) {
      setFormError(
        error instanceof ApiError ? error.detail : "Authentication failed.",
      );
    } finally {
      setSubmitting(false);
    }
  };

  const switchMode = (next: Mode) => {
    setMode(next);
    setFormError(null);
  };

  return (
    <div className="flex items-center justify-center">
      <form
        onSubmit={handleSubmit}
        className="bg-white p-6 rounded shadow-lg w-full max-w-sm"
      >
        <h2 className="text-xl mb-4">Welcome to the Game!</h2>
        <div className="flex gap-2 mb-4">
          <button
            type="button"
            onClick={() => switchMode("login")}
            className={`flex-1 py-2 px-4 rounded ${
              mode === "login"
                ? "bg-blue-500 text-white"
                : "bg-gray-200 text-gray-700"
            }`}
          >
            Login
          </button>
          <button
            type="button"
            onClick={() => switchMode("register")}
            className={`flex-1 py-2 px-4 rounded ${
              mode === "register"
                ? "bg-blue-500 text-white"
                : "bg-gray-200 text-gray-700"
            }`}
          >
            Register
          </button>
        </div>
        <label className="block mb-2 text-sm font-medium" htmlFor="auth-name">
          Name
        </label>
        <input
          id="auth-name"
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Enter your name"
          className="border-2 py-2 px-4 mb-4 rounded-md w-full"
          autoComplete="username"
          required
        />
        <label
          className="block mb-2 text-sm font-medium"
          htmlFor="auth-password"
        >
          Password
        </label>
        <input
          id="auth-password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder={
            mode === "register" ? "8 to 100 characters" : "Enter your password"
          }
          className="border-2 py-2 px-4 mb-4 rounded-md w-full"
          autoComplete={
            mode === "register" ? "new-password" : "current-password"
          }
          required
        />
        {formError && (
          <p role="alert" className="text-red-500 mb-4 text-sm">
            {formError}
          </p>
        )}
        <button
          type="submit"
          disabled={submitting}
          className="bg-blue-500 text-white py-2 px-4 rounded w-full disabled:opacity-50"
        >
          {submitting
            ? "Please wait..."
            : mode === "login"
              ? "Login"
              : "Create account"}
        </button>
      </form>
    </div>
  );
}

const PlayerGreeting = () => {
  return (
    <AuthProvider>
      <Suspense
        fallback={
          <div className="flex items-center justify-center">
            <p className="bg-white p-6 rounded shadow-lg">Loading...</p>
          </div>
        }
      >
        <AuthForm />
      </Suspense>
    </AuthProvider>
  );
};

export default PlayerGreeting;
