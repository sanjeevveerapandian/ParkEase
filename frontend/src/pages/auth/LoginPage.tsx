import { useState } from "react";
import type { FormEvent } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { Mail, Lock, Car, Building2 } from "lucide-react";
import { useAuth } from "../../context/AuthContext";
import { getErrorMessage } from "../../api/errors";
import AuthLayout from "../../components/AuthLayout";
import { Button, ErrorMessage, Field, TextInput } from "../../components/ui";
import type { Role } from "../../types";

function homePathForRole(role: Role) {
  return role === "Driver" ? "/driver/lots" : "/operator/lots";
}

// One-click logins for the seeded demo accounts. Only shown when the deployment opts in with
// VITE_DEMO_LOGINS=true, so a real installation never advertises credentials.
const SHOW_DEMO_LOGINS = import.meta.env.VITE_DEMO_LOGINS === "true";
const DEMO_ACCOUNTS = [
  { label: "Try as a Driver", email: "driver@parkease.com", password: "Driver@123", icon: Car },
  { label: "Try as an Operator", email: "operator@parkease.com", password: "Operator@123", icon: Building2 },
];

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    await signIn(email, password);
  }

  async function signIn(emailValue: string, passwordValue: string) {
    setError("");
    setSubmitting(true);
    try {
      const role = await login({ email: emailValue, password: passwordValue });
      const redirectTo = (location.state as { from?: string } | null)?.from;
      navigate(redirectTo ?? homePathForRole(role), { replace: true });
    } catch (err) {
      setError(getErrorMessage(err, "Invalid email or password."));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <AuthLayout title="Welcome back" subtitle="Log in to manage your parking.">
      <form onSubmit={handleSubmit}>
        <ErrorMessage>{error}</ErrorMessage>
        <Field label="Email" htmlFor="email">
          <div className="relative">
            <Mail size={16} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
            <TextInput
              id="email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
              className="pl-10"
            />
          </div>
        </Field>
        <Field label="Password" htmlFor="password">
          <div className="relative">
            <Lock size={16} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
            <TextInput
              id="password"
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              className="pl-10"
            />
          </div>
        </Field>
        <Button type="submit" className="w-full" loading={submitting}>
          {submitting ? "Logging in…" : "Log in"}
        </Button>
      </form>
      {SHOW_DEMO_LOGINS && (
        <div className="mt-6 rounded-xl border border-indigo-100 bg-indigo-50/60 p-4">
          <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-indigo-600">Demo accounts</p>
          <div className="grid gap-2 sm:grid-cols-2">
            {DEMO_ACCOUNTS.map(({ label, email: demoEmail, password: demoPassword, icon: Icon }) => (
              <Button
                key={demoEmail}
                type="button"
                variant="secondary"
                icon={<Icon size={15} />}
                disabled={submitting}
                onClick={() => signIn(demoEmail, demoPassword)}
              >
                {label}
              </Button>
            ))}
          </div>
          <p className="mt-3 text-xs text-slate-500">
            Sample data for exploring the app. Please don't enter anything private.
          </p>
        </div>
      )}
      <p className="mt-6 text-center text-sm text-slate-500">
        Don't have an account?{" "}
        <Link to="/register" className="font-medium text-indigo-600 hover:text-indigo-500">
          Register
        </Link>
      </p>
    </AuthLayout>
  );
}
