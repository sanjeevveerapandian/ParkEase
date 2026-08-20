import { Link, useLocation, useNavigate } from "react-router-dom";
import { LayoutGrid, LogOut, ParkingSquare, ReceiptText, BarChart3 } from "lucide-react";
import { useAuth } from "../context/AuthContext";

function NavLink({
  to,
  active,
  icon,
  children,
}: {
  to: string;
  active: boolean;
  icon: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <Link
      to={to}
      className={`inline-flex items-center gap-1.5 rounded-lg px-2.5 py-2 text-sm font-medium transition-colors sm:px-3 ${
        active ? "bg-indigo-50 text-indigo-700" : "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
      }`}
    >
      {icon}
      <span className="hidden sm:inline">{children}</span>
    </Link>
  );
}

function initials(name: string) {
  const parts = name.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? "") + (parts[1]?.[0] ?? "")).toUpperCase();
}

export default function Navbar() {
  const { fullName, role, logout, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const homePath = role === "Driver" ? "/driver/lots" : "/operator/lots";

  function handleLogout() {
    logout();
    navigate("/login", { replace: true });
  }

  return (
    <nav className="sticky top-0 z-40 border-b border-slate-200 bg-white/85 backdrop-blur-md">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3 sm:px-6 lg:px-8">
        <Link to={isAuthenticated ? homePath : "/login"} className="flex items-center gap-2">
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-600 text-white">
            <ParkingSquare size={18} />
          </span>
          <span className="text-lg font-bold tracking-tight text-slate-900">ParkEase</span>
        </Link>

        {isAuthenticated && (
          <div className="flex items-center gap-1 sm:gap-2">
            <div className="flex items-center gap-1">
              {role === "Driver" && (
                <>
                  <NavLink to="/driver/lots" active={location.pathname.startsWith("/driver/lots")} icon={<LayoutGrid size={15} />}>
                    Find Parking
                  </NavLink>
                  <NavLink to="/driver/bookings" active={location.pathname === "/driver/bookings"} icon={<ReceiptText size={15} />}>
                    My Bookings
                  </NavLink>
                </>
              )}
              {(role === "Operator" || role === "Admin") && (
                <NavLink to="/operator/lots" active={location.pathname.startsWith("/operator")} icon={<ParkingSquare size={15} />}>
                  Lots
                </NavLink>
              )}
              {role === "Admin" && (
                <NavLink to="/admin/reports" active={location.pathname === "/admin/reports"} icon={<BarChart3 size={15} />}>
                  Reports
                </NavLink>
              )}
            </div>

            <div className="ml-2 flex items-center gap-3 border-l border-slate-200 pl-3 sm:pl-4">
              <div className="hidden items-center gap-2 sm:flex">
                <span className="flex h-8 w-8 items-center justify-center rounded-full bg-slate-800 text-xs font-semibold text-white">
                  {initials(fullName ?? "?")}
                </span>
                <div className="leading-tight">
                  <p className="text-sm font-medium text-slate-800">{fullName}</p>
                  <p className="text-xs text-slate-400">{role}</p>
                </div>
              </div>
              <button
                onClick={handleLogout}
                className="inline-flex items-center gap-1.5 rounded-lg bg-slate-900 px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700"
              >
                <LogOut size={14} />
                <span className="hidden sm:inline">Logout</span>
              </button>
            </div>
          </div>
        )}
      </div>
    </nav>
  );
}
