import axios from "axios";
import type { ApiProblemDetails } from "../types";


export function getErrorMessage(err: unknown, fallback = "Something went wrong. Please try again."): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as (ApiProblemDetails & { message?: string; errors?: Record<string, string[]> }) | string | undefined;
    if (typeof data === "string" && data.trim()) return data;
    if (data && typeof data === "object") {
      if (typeof data.message === "string" && data.message) return data.message;
      if (data.errors && typeof data.errors === "object") {
        const firstField = Object.values(data.errors)[0];
        if (Array.isArray(firstField) && firstField[0]) return firstField[0];
      }
      if (typeof data.detail === "string" && data.detail) return data.detail;
      if (typeof data.title === "string" && data.title) return data.title;
    }
    if (err.message) return err.message;
  }
  if (err instanceof Error) return err.message;
  return fallback;
}
