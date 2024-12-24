'use client';

import { useEffect } from "react";
import { redirect, useRouter } from "next/navigation";

export default function isAuth(Component: any) {
  const isAuthenticated = () => {
    const playerName = localStorage.getItem("playerName");
    return playerName ? true : false;
  };
  const auth = isAuthenticated();

  return function IsAuth(props: any) {
    useEffect(() => {
      if (!auth) {
        return redirect("/");
      }
    }, []);

    if (!auth) {
      return null;
    }

    return <Component {...props} />;
  };
}
