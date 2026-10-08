"use client";

import { useRouter, usePathname } from "next/navigation";
import { useEffect } from "react";
import type { ComponentType, FC } from "react";
import { AuthProvider, useAuth } from "@/lib/auth";

function AuthGate<P extends object>({
  Component,
  props,
}: {
  Component: ComponentType<P>;
  props: P;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const { player, loading } = useAuth();

  useEffect(() => {
    if (!loading && !player) {
      router.push(`/?returnUrl=${encodeURIComponent(pathname)}`);
    }
  }, [loading, player, pathname, router]);

  if (loading) {
    return <div className="p-4">Checking sign in...</div>;
  }

  if (!player) {
    return null;
  }

  return <Component {...props} />;
}

export default function isAuth<P extends object>(
  Component: ComponentType<P>,
): FC<P> {
  return function IsAuth(props: P) {
    return (
      <AuthProvider>
        <AuthGate Component={Component} props={props} />
      </AuthProvider>
    );
  };
}
