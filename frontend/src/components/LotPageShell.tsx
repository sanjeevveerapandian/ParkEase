import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, MapPin } from "lucide-react";
import { ErrorMessage, Skeleton, SkeletonGrid } from "./ui";
import type { ParkingLotDto } from "../types";

// Shared "back link + loading skeleton + lot name/address header" shell for the two
// per-lot detail pages (driver's slot picker, operator's lot management) — previously
// duplicated near-verbatim between them. Each page still owns its own body content,
// passed as children, since that differs completely between the two.
export function LotPageShell({
  backTo,
  lot,
  lotId,
  loading,
  error,
  children,
}: {
  backTo: string;
  lot: ParkingLotDto | null;
  lotId: number;
  loading: boolean;
  error: string;
  children: ReactNode;
}) {
  return (
    <>
      <Link
        to={backTo}
        className="mb-4 inline-flex items-center gap-1.5 text-sm font-medium text-slate-500 hover:text-slate-900"
      >
        <ArrowLeft size={15} /> Back to lots
      </Link>

      <ErrorMessage>{error}</ErrorMessage>

      {loading ? (
        <div className="space-y-6">
          <Skeleton className="h-8 w-64" />
          <SkeletonGrid count={4} />
        </div>
      ) : (
        <>
          <div className="mb-6">
            <h1 className="text-2xl font-bold tracking-tight text-slate-900">{lot?.name ?? `Lot #${lotId}`}</h1>
            {lot && (
              <p className="mt-1 flex items-center gap-1.5 text-sm text-slate-500">
                <MapPin size={14} /> {lot.address}
              </p>
            )}
          </div>
          {children}
        </>
      )}
    </>
  );
}
