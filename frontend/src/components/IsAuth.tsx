'use client';

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { ComponentType, FC } from "react";

export default function isAuth<P extends object>(Component: ComponentType<P>): FC<P> {
  return function IsAuth(props: P) {
    const router = useRouter();
    const [isAuthorized, setIsAuthorized] = useState<boolean | null>(null);

    useEffect(() => {
      const playerName = localStorage.getItem("playerName");
      setIsAuthorized(!!playerName);
      
      if (!playerName) {
        router.push('/');
      }
    }, [router]);

    if (isAuthorized === null) {
      return null; // Initial loading state
    }

    if (!isAuthorized) {
      return null;
    }

    return <Component {...props} />;
  };
}
