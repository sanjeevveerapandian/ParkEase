import type { ReactNode } from "react";
import { ParkingSquare, ShieldCheck, Zap, BarChart3 } from "lucide-react";

const features = [
  { icon: Zap, text: "Real-time slot availability across every lot" },
  { icon: ShieldCheck, text: "Role-based access for Drivers, Operators & Admins" },
  { icon: BarChart3, text: "Live occupancy and revenue reporting" },
];

export default function AuthLayout({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle: string;
  children: ReactNode;
}) {
  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-2">
      {/* Branded panel — hidden on small screens to keep the form front and center */}
      <div className="relative hidden flex-col justify-between overflow-hidden bg-gradient-to-br from-indigo-600 via-indigo-700 to-slate-900 p-12 text-white lg:flex">
        <div
          className="pointer-events-none absolute inset-0 opacity-[0.07]"
          style={{
            backgroundImage:
              "radial-gradient(circle at 1px 1px, white 1px, transparent 0)",
            backgroundSize: "28px 28px",
          }}
        />
        <div className="relative flex items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-white/15">
            <ParkingSquare size={20} />
          </span>
          <span className="text-xl font-bold tracking-tight">ParkEase</span>
        </div>

        <div className="relative">
          <h2 className="max-w-md text-3xl font-bold leading-tight tracking-tight">
            Smart parking management, built for the whole city.
          </h2>
          <ul className="mt-8 space-y-4">
            {features.map(({ icon: Icon, text }) => (
              <li key={text} className="flex items-center gap-3 text-sm text-indigo-100">
                <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-white/10">
                  <Icon size={16} />
                </span>
                {text}
              </li>
            ))}
          </ul>
        </div>

        <p className="relative text-xs text-indigo-200/70">© {new Date().getFullYear()} ParkEase</p>
      </div>

      {/* Form panel */}
      <div className="flex flex-col items-center justify-center bg-slate-50 px-4 py-12 sm:px-6">
        <div className="w-full max-w-sm">
          <div className="mb-8 flex items-center gap-2 lg:hidden">
            <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-600 text-white">
              <ParkingSquare size={18} />
            </span>
            <span className="text-lg font-bold tracking-tight text-slate-900">ParkEase</span>
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">{title}</h1>
          <p className="mt-1 mb-8 text-sm text-slate-500">{subtitle}</p>
          {children}
        </div>
      </div>
    </div>
  );
}
