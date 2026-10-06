import { createContext, useCallback, useContext, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { authApi } from "../api/endpoints";
import { setAuthToken } from "../api/client";
import type { LoginDto, RegisterDto, Role } from "../types";

interface AuthState {
  token: string | null;
  role: Role | null;
  fullName: string | null;
}

interface AuthContextValue extends AuthState {
  isAuthenticated: boolean;
  login: (data: LoginDto) => Promise<Role>;
  register: (data: RegisterDto) => Promise<Role>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);


export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => {
    const token = localStorage.getItem("token");
    const role = localStorage.getItem("role") as Role | null;
    const fullName = localStorage.getItem("fullName");
    if (token) setAuthToken(token); // so the axios interceptor picks it up immediately
    return { token, role, fullName };
  });

  const login = useCallback(async (data: LoginDto) => {
    const res = await authApi.login(data);
    const { token, role, fullName } = res.data;
    setAuthToken(token);
    localStorage.setItem("token", token);
    localStorage.setItem("role", role);
    localStorage.setItem("fullName", fullName);
    setState({ token, role: role as Role, fullName });
    return role as Role;
  }, []);

  const register = useCallback(async (data: RegisterDto) => {
    const res = await authApi.register(data);
    const { token, role, fullName } = res.data;
    setAuthToken(token);
    localStorage.setItem("token", token);
    localStorage.setItem("role", role);
    localStorage.setItem("fullName", fullName);
    setState({ token, role: role as Role, fullName });
    return role as Role;
  }, []);

  const logout = useCallback(() => {
    setAuthToken(null);
    localStorage.removeItem("token");
    localStorage.removeItem("role");
    localStorage.removeItem("fullName");
    setState({ token: null, role: null, fullName: null });
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      ...state,
      isAuthenticated: !!state.token,
      login,
      register,
      logout,
    }),
    [state, login, register, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
