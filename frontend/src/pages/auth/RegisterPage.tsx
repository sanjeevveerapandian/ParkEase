import { useState } from "react";
import type { FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { User, Mail, Lock, Phone } from "lucide-react";
import { useAuth } from "../../context/AuthContext";
import { getErrorMessage } from "../../api/errors";
import AuthLayout from "../../components/AuthLayout";
import { Button, ErrorMessage, Field, Select, TextInput } from "../../components/ui";
import { isValidIndianMobile } from "../../lib/booking";
import type { Role } from "../../types";

function homePathForRole(role: Role) {
  return role === "Driver" ? "/driver/lots" : "/operator/lots";
}

export default function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [role, setRole] = useState<Role>("Driver");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError("");
    if (!isValidIndianMobile(phoneNumber)) {
      setError("Enter a valid mobile number, e.g. 9876543210. Booking alerts are sent to it by SMS.");
      return;
    }
    setSubmitting(true);
    try {
      const assignedRole = await register({ fullName, email, password, phoneNumber, role });
      navigate(homePathForRole(assignedRole), { replace: true });
    } catch (err) {
      setError(getErrorMessage(err, "Registration failed."));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <AuthLayout title="Create your account" subtitle="Get started with ParkEase in seconds.">
      <form onSubmit={handleSubmit}>
        <ErrorMessage>{error}</ErrorMessage>
        <Field label="Full name" htmlFor="fullName">
          <div className="relative">
            <User size={16} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
            <TextInput
              id="fullName"
              required
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              placeholder="Sanjeev Veerapandian"
              className="pl-10"
            />
          </div>
        </Field>
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
        <Field label="Phone number" htmlFor="phoneNumber">
          <div className="relative">
            <Phone size={16} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
            <TextInput
              id="phoneNumber"
              required
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              placeholder="9876543210"
              className="pl-10"
            />
          </div>
          <p className="mt-1 text-xs text-slate-400">We text booking confirmations and receipts to this number.</p>
        </Field>
        <Field label="Role" htmlFor="role">
          <Select id="role" value={role} onChange={(e) => setRole(e.target.value as Role)}>
            <option value="Driver">Driver</option>
            <option value="Operator">Operator</option>
            <option value="Admin">Admin</option>
          </Select>
        </Field>
        <Button type="submit" className="w-full" loading={submitting}>
          {submitting ? "Creating account…" : "Register"}
        </Button>
      </form>
      <p className="mt-6 text-center text-sm text-slate-500">
        Already have an account?{" "}
        <Link to="/login" className="font-medium text-indigo-600 hover:text-indigo-500">
          Log in
        </Link>
      </p>
    </AuthLayout>
  );
}
