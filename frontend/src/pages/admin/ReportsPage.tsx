import { useEffect, useState } from "react";
import {
  Building2,
  ParkingSquare,
  CheckCircle2,
  XCircle,
  Gauge,
  IndianRupee,
  BarChart3,
} from "lucide-react";
import { reportsApi } from "../../api/endpoints";
import { getErrorMessage } from "../../api/errors";
import {
  Card,
  EmptyState,
  ErrorMessage,
  PageContainer,
  PageHeader,
  Skeleton,
  StatCard,
} from "../../components/ui";
import type { ReportOverviewDto } from "../../types";

function LotOccupancyBar({ lotName, pct, revenue }: { lotName: string; pct: number; revenue: number }) {
  return (
    <div>
      <div className="mb-1 flex items-center justify-between text-sm">
        <span className="font-medium text-slate-800">{lotName}</span>
        <span className="text-slate-500">
          {pct.toFixed(1)}% · <span className="font-medium text-slate-700">₹{revenue.toFixed(2)}</span>
        </span>
      </div>
      <div className="h-2.5 w-full overflow-hidden rounded-full bg-slate-100">
        <div
          className={`h-full rounded-full transition-all ${
            pct >= 80 ? "bg-rose-500" : pct >= 40 ? "bg-amber-500" : "bg-indigo-500"
          }`}
          style={{ width: `${Math.min(pct, 100)}%` }}
        />
      </div>
    </div>
  );
}

export default function ReportsPage() {
  const [report, setReport] = useState<ReportOverviewDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    reportsApi
      .overview()
      .then((res) => setReport(res.data))
      .catch((err) => setError(getErrorMessage(err, "Could not load report.")))
      .finally(() => setLoading(false));
  }, []);

  return (
    <PageContainer>
      <PageHeader title="Reports Overview" description="System-wide occupancy and revenue at a glance." />
      <ErrorMessage>{error}</ErrorMessage>

      {loading ? (
        <div className="space-y-6">
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-24" />
            ))}
          </div>
          <Skeleton className="h-64" />
        </div>
      ) : report ? (
        <>
          <div className="mb-6 grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
            <StatCard icon={<Building2 size={18} />} label="Total Lots" value={report.totalLots} tone="indigo" />
            <StatCard icon={<ParkingSquare size={18} />} label="Total Slots" value={report.totalSlots} tone="slate" />
            <StatCard icon={<XCircle size={18} />} label="Occupied" value={report.occupiedSlots} tone="rose" />
            <StatCard icon={<CheckCircle2 size={18} />} label="Available" value={report.availableSlots} tone="emerald" />
            <StatCard
              icon={<Gauge size={18} />}
              label="Occupancy Rate"
              value={`${report.overallOccupancyRatePercent.toFixed(1)}%`}
              tone="amber"
            />
            <StatCard
              icon={<IndianRupee size={18} />}
              label="Total Revenue"
              value={`₹${report.totalRevenue.toFixed(2)}`}
              tone="indigo"
            />
          </div>

          <Card>
            <h2 className="mb-5 flex items-center gap-1.5 text-base font-semibold text-slate-900">
              <BarChart3 size={16} /> Lot Breakdown
            </h2>
            {report.lotBreakdown.length === 0 ? (
              <EmptyState icon={<Building2 size={20} />} title="No lots yet" />
            ) : (
              <div className="space-y-5">
                {report.lotBreakdown.map((lot) => (
                  <LotOccupancyBar
                    key={lot.lotId}
                    lotName={lot.lotName}
                    pct={lot.occupancyRatePercent}
                    revenue={lot.revenue}
                  />
                ))}
              </div>
            )}
          </Card>
        </>
      ) : null}
    </PageContainer>
  );
}
