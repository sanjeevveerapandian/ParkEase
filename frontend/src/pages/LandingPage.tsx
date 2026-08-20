import { Link } from "react-router-dom";
import {
  ParkingSquare,
  Zap,
  ShieldCheck,
  BarChart3,
  ArrowRight,
  Search,
  CreditCard,
  Building2,
  Car,
} from "lucide-react";

const features = [
  {
    icon: Zap,
    title: "Real-time availability",
    description: "See exactly which slots are open across every lot, updated the moment someone books or exits.",
  },
  {
    icon: ShieldCheck,
    title: "Role-based dashboards",
    description: "Drivers book slots, Operators manage their lots, Admins see the whole system — each gets exactly what they need.",
  },
  {
    icon: CreditCard,
    title: "Automated billing",
    description: "Fees are calculated automatically on exit, including overstay penalties — no manual math, no disputes.",
  },
  {
    icon: BarChart3,
    title: "Live reporting",
    description: "Occupancy rates and revenue, broken down per lot, so operators and admins always know what's happening.",
  },
];

const steps = [
  { icon: Search, title: "Find a lot", description: "Search parking lots by name or address and check live availability." },
  { icon: Car, title: "Book & park", description: "Reserve an open slot in seconds and park your vehicle." },
  { icon: CreditCard, title: "Exit & pay", description: "Check out when you leave — your fee is calculated automatically." },
];

export default function LandingPage() {
  return (
    <div className="min-h-screen bg-white">
      {/* Public nav */}
      <nav className="border-b border-slate-200">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4 sm:px-6 lg:px-8">
          <div className="flex items-center gap-2">
            <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-600 text-white">
              <ParkingSquare size={18} />
            </span>
            <span className="text-lg font-bold tracking-tight text-slate-900">ParkEase</span>
          </div>
          <div className="flex items-center gap-2">
            <Link
              to="/login"
              className="rounded-lg px-4 py-2 text-sm font-medium text-slate-600 hover:text-slate-900"
            >
              Log in
            </Link>
            <Link
              to="/register"
              className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-indigo-500"
            >
              Get Started
            </Link>
          </div>
        </div>
      </nav>

      {/* Hero */}
      <section className="relative overflow-hidden">
        <div
          className="pointer-events-none absolute inset-0 opacity-[0.4]"
          style={{
            backgroundImage: "radial-gradient(circle at 1px 1px, #e2e8f0 1px, transparent 0)",
            backgroundSize: "28px 28px",
            maskImage: "linear-gradient(to bottom, black, transparent)",
          }}
        />
        <div className="relative mx-auto max-w-4xl px-4 py-24 text-center sm:px-6 lg:px-8">
          <span className="mb-6 inline-flex items-center gap-1.5 rounded-full bg-indigo-50 px-3 py-1 text-xs font-medium text-indigo-700 ring-1 ring-inset ring-indigo-600/20">
            <Zap size={12} /> Real-time parking, made simple
          </span>
          <h1 className="text-4xl font-bold tracking-tight text-slate-900 sm:text-5xl">
            Smart parking management,
            <br />
            built for the whole city.
          </h1>
          <p className="mx-auto mt-5 max-w-xl text-lg text-slate-500">
            Find, book, and pay for parking in seconds — or manage every lot you run from one live dashboard.
          </p>
          <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
            <Link
              to="/register"
              className="inline-flex items-center gap-1.5 rounded-lg bg-indigo-600 px-5 py-3 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-indigo-500"
            >
              Get Started Free <ArrowRight size={16} />
            </Link>
            <Link
              to="/login"
              className="inline-flex items-center gap-1.5 rounded-lg border border-slate-300 px-5 py-3 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-50"
            >
              Log in
            </Link>
          </div>
        </div>
      </section>

      {/* How it works */}
      <section className="mx-auto max-w-6xl px-4 py-16 sm:px-6 lg:px-8">
        <h2 className="text-center text-2xl font-bold tracking-tight text-slate-900">How it works</h2>
        <div className="mt-10 grid grid-cols-1 gap-8 sm:grid-cols-3">
          {steps.map(({ icon: Icon, title, description }, i) => (
            <div key={title} className="relative text-center">
              <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-indigo-600 text-white shadow-sm">
                <Icon size={22} />
              </div>
              <p className="mt-4 text-xs font-semibold uppercase tracking-wide text-indigo-500">Step {i + 1}</p>
              <p className="mt-1 text-base font-semibold text-slate-900">{title}</p>
              <p className="mt-1.5 text-sm text-slate-500">{description}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Features */}
      <section className="bg-slate-50 py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <h2 className="text-center text-2xl font-bold tracking-tight text-slate-900">
            Everything you need to run parking
          </h2>
          <div className="mt-10 grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {features.map(({ icon: Icon, title, description }) => (
              <div key={title} className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
                <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600">
                  <Icon size={18} />
                </div>
                <p className="mt-4 text-base font-semibold text-slate-900">{title}</p>
                <p className="mt-1.5 text-sm text-slate-500">{description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA band */}
      <section className="mx-auto max-w-6xl px-4 py-16 sm:px-6 lg:px-8">
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-indigo-600 via-indigo-700 to-slate-900 px-8 py-14 text-center text-white">
          <Building2 className="mx-auto mb-4 opacity-80" size={28} />
          <h2 className="text-2xl font-bold tracking-tight sm:text-3xl">Run a parking lot? Get set up in minutes.</h2>
          <p className="mx-auto mt-3 max-w-md text-indigo-100">
            Register as an Operator, add your lot and slots, and start tracking bookings and revenue immediately.
          </p>
          <Link
            to="/register"
            className="mt-6 inline-flex items-center gap-1.5 rounded-lg bg-white px-5 py-3 text-sm font-semibold text-indigo-700 shadow-sm transition-colors hover:bg-indigo-50"
          >
            Create your account <ArrowRight size={16} />
          </Link>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-slate-200 py-8">
        <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-3 px-4 text-sm text-slate-500 sm:flex-row sm:px-6 lg:px-8">
          <div className="flex items-center gap-2">
            <span className="flex h-6 w-6 items-center justify-center rounded-md bg-indigo-600 text-white">
              <ParkingSquare size={13} />
            </span>
            <span className="font-medium text-slate-700">ParkEase</span>
          </div>
          <p>© {new Date().getFullYear()} ParkEase. All rights reserved.</p>
        </div>
      </footer>
    </div>
  );
}
