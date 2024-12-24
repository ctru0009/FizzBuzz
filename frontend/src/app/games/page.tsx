"use client";
import GameList from "@/components/GameList";
import isAuth from "@/components/IsAuth";
import React from "react";

const page = () => {
  return <div>
    <GameList />
  </div>;
};

export default isAuth(page);
