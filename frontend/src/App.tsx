import { Navigate, Route, Routes } from "react-router-dom";
import AppLayout from "./components/AppLayout";
import RequireRole from "./components/RequireRole";
import { useAuth } from "./context/AuthContext";
import LandingPage from "./pages/LandingPage";
import LoginPage from "./pages/auth/LoginPage";
import RegisterPage from "./pages/auth/RegisterPage";
import AccessDeniedPage from "./pages/AccessDeniedPage";
import BrowseLotsPage from "./pages/driver/BrowseLotsPage";
import LotSlotsPage from "./pages/driver/LotSlotsPage";
import MyBookingsPage from "./pages/driver/MyBookingsPage";
import LotsPage from "./pages/operator/LotsPage";
import LotDetailPage from "./pages/operator/LotDetailPage";
import ReportsPage from "./pages/admin/ReportsPage";

function HomeRoute() {
  const { isAuthenticated, role } = useAuth();
  if (isAuthenticated) {
    return <Navigate to={role === "Driver" ? "/driver/lots" : "/operator/lots"} replace />;
  }
  return <LandingPage />;
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<HomeRoute />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<AppLayout />}>
        <Route path="/access-denied" element={<AccessDeniedPage />} />

        <Route
          path="/driver/lots"
          element={
            <RequireRole allowedRoles={["Driver"]}>
              <BrowseLotsPage />
            </RequireRole>
          }
        />
        <Route
          path="/driver/lots/:id"
          element={
            <RequireRole allowedRoles={["Driver"]}>
              <LotSlotsPage />
            </RequireRole>
          }
        />
        <Route
          path="/driver/bookings"
          element={
            <RequireRole allowedRoles={["Driver"]}>
              <MyBookingsPage />
            </RequireRole>
          }
        />

        <Route
          path="/operator/lots"
          element={
            <RequireRole allowedRoles={["Operator", "Admin"]}>
              <LotsPage />
            </RequireRole>
          }
        />
        <Route
          path="/operator/lots/:id"
          element={
            <RequireRole allowedRoles={["Operator", "Admin"]}>
              <LotDetailPage />
            </RequireRole>
          }
        />

        <Route
          path="/admin/reports"
          element={
            <RequireRole allowedRoles={["Admin"]}>
              <ReportsPage />
            </RequireRole>
          }
        />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
