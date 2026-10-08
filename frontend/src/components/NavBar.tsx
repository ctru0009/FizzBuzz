"use client";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { AuthProvider, useAuth } from "@/lib/auth";

function NavBarContent() {
  const router = useRouter();
  const { player, loading, logout } = useAuth();

  const handleLogout = async () => {
    await logout();
    router.push("/");
  };

  return (
    <div className="flex items-center p-2 bg-gray-100 border-b border-gray-300">
      <div className="flex-grow font-bold text-2xl mx-10">
        <Link href="/">FizzBuzz Game</Link>
      </div>
      {!loading && player && (
        <span className="mr-4 text-sm">Signed in as {player.name}</span>
      )}
      {!loading && player && (
        <button onClick={handleLogout} className="ml-4 mr-10">
          Logout
        </button>
      )}
      {!loading && !player && (
        <Link href="/" className="ml-4 mr-10">
          Login
        </Link>
      )}
    </div>
  );
}

const NavBar = () => {
  return (
    <AuthProvider>
      <NavBarContent />
    </AuthProvider>
  );
};

export default NavBar;
