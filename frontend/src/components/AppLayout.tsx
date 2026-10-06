import { Outlet } from "react-router-dom";
import Navbar from "./Navbar";

export default function AppLayout() {
  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar />
      <div className="animate-fade-slide-in">
        <Outlet />
      </div>
    </div>
  );
}
