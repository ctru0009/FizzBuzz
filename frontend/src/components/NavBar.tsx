"use client";
import Link from "next/link";
import { useRouter } from "next/navigation";
import React, { useState } from "react";

const NavBar = () => {
  const [isLogin, setIsLogin] = useState(false);
  const router = useRouter();

  const handleLogout = () => {
    localStorage.removeItem("playerName");
    setIsLogin(false); 
    router.push("/");
  };

  return (
    <div className="flex items-center p-2 bg-gray-100 border-b border-gray-300">
      <div className="flex-grow font-bold text-2xl mx-10">
        <Link href="/">FizzBuzz Game</Link>
      </div>
      {isLogin && (<button onClick={handleLogout} className="ml-4">
        Logout
      </button>)}
    </div>
  );
};

export default NavBar;
