import axios from "axios";
import type { ApiProblemDetails } from "../types";

// Backend error bodies come in three shapes depending on the failure:
//  - hand-rolled controller errors: { message: "..." }               (e.g. AuthController, BookingsController)
//  - [ApiController] model validation: { errors: { field: [...] } }  (BadRequest(ModelState))
//  - GlobalExceptionMiddleware on unhandled exceptions: { statusCode, message, correlationId }
// Pull out the most useful human-readable string for display, in that order.
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
