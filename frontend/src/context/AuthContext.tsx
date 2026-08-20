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

// Deliberately in-memory only (no localStorage) per spec: token/role/fullName
// live in React state and are lost on a full page reload.
export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    token: null,
    role: null,
    fullName: null,
  });

  const login = useCallback(async (data: LoginDto) => {
    const res = await authApi.login(data);
    const { token, role, fullName } = res.data;
    setAuthToken(token);
    setState({ token, role: role as Role, fullName });
    return role as Role;
  }, []);

  const register = useCallback(async (data: RegisterDto) => {
    const res = await authApi.register(data);
    const { token, role, fullName } = res.data;
    setAuthToken(token);
    setState({ token, role: role as Role, fullName });
    return role as Role;
  }, []);

  const logout = useCallback(() => {
    setAuthToken(null);
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
