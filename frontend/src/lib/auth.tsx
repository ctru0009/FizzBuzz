"use client";

import { createContext, useContext, useEffect, useState } from "react";
import type { ReactNode } from "react";
import { getJson, postJson } from "./api";
import type Player from "@/types/Player";

interface AuthPlayer {
  id: number;
  name: string;
}

interface AuthContextValue {
  player: AuthPlayer | null;
  loading: boolean;
  login(_name: string, _password: string): Promise<void>;
  register(_name: string, _password: string): Promise<void>;
  logout(): Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const AUTH_CHANGE_EVENT = "fizzbuzz:auth-change";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [player, setPlayer] = useState<AuthPlayer | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      try {
        const me = await getJson<Player>("/players/me");
        if (!cancelled) {
          setPlayer({ id: me.id, name: me.name });
        }
      } catch {
        if (!cancelled) {
          setPlayer(null);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };
    load();
    const onAuthChange = (event: Event) => {
      const detail = (event as CustomEvent<AuthPlayer | null>).detail;
      if (!cancelled) {
        setPlayer(detail ?? null);
        setLoading(false);
      }
    };
    window.addEventListener(AUTH_CHANGE_EVENT, onAuthChange);
    return () => {
      cancelled = true;
      window.removeEventListener(AUTH_CHANGE_EVENT, onAuthChange);
    };
  }, []);

  const broadcast = (next: AuthPlayer | null) => {
    setPlayer(next);
    setLoading(false);
    window.dispatchEvent(
      new CustomEvent<AuthPlayer | null>(AUTH_CHANGE_EVENT, { detail: next }),
    );
  };

  const login = async (name: string, password: string): Promise<void> => {
    const result = await postJson<Player>("/players/login", { name, password });
    broadcast({ id: result.id, name: result.name });
  };

  const register = async (name: string, password: string): Promise<void> => {
    const result = await postJson<Player>("/players/register", {
      name,
      password,
    });
    broadcast({ id: result.id, name: result.name });
  };

  const logout = async (): Promise<void> => {
    try {
      await postJson<unknown>("/players/logout", {});
    } catch {
      // Cookie may already be expired, local state still clears below.
    }
    broadcast(null);
  };

  return (
    <AuthContext.Provider value={{ player, loading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider.");
  }
  return context;
}
