import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Search, MapPin, Building2, ArrowRight } from "lucide-react";
import { parkingLotsApi } from "../../api/endpoints";
import { getErrorMessage } from "../../api/errors";
import {
  Badge,
  Card,
  EmptyState,
  ErrorMessage,
  PageContainer,
  PageHeader,
  Select,
  SkeletonGrid,
  TextInput,
} from "../../components/ui";
import type { ParkingLotDto } from "../../types";

type SortOption = "available" | "name";

function LotCard({ lot, onSelect }: { lot: ParkingLotDto; onSelect: (lot: ParkingLotDto) => void }) {
  const full = lot.availableSlots === 0;
  return (
    <button
      onClick={() => onSelect(lot)}
      disabled={full}
      className={`group flex flex-col items-start rounded-2xl border p-5 text-left transition-shadow ${
        full
          ? "cursor-not-allowed border-slate-200 bg-slate-50 opacity-70"
          : "border-slate-200 bg-white shadow-sm hover:shadow-md hover:border-indigo-200"
      }`}
    >
      <div className="flex w-full items-start justify-between">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600">
          <Building2 size={18} />
        </div>
        <Badge tone={full ? "red" : "green"} dot>
          {full ? "Full" : `${lot.availableSlots} open`}
        </Badge>
      </div>

      <p className="mt-3 text-base font-semibold text-slate-900 group-hover:text-indigo-600">{lot.name}</p>
      <p className="mt-1 flex items-center gap-1 text-sm text-slate-500">
        <MapPin size={13} className="shrink-0" />
        <span className="truncate">{lot.address}</span>
      </p>

      <p className="mt-3 flex items-center gap-1 text-sm font-medium text-indigo-600">
        {full ? "No slots available" : "View slots"}
        {!full && <ArrowRight size={14} />}
      </p>
    </button>
  );
}

export default function BrowseLotsPage() {
  const navigate = useNavigate();
  const [lots, setLots] = useState<ParkingLotDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [query, setQuery] = useState("");
  const [sortBy, setSortBy] = useState<SortOption>("available");
  const [hideFull, setHideFull] = useState(false);

  useEffect(() => {
    parkingLotsApi
      .browse()
      .then((res) => setLots(res.data))
      .catch((err) => setError(getErrorMessage(err, "Could not load parking lots.")))
      .finally(() => setLoading(false));
  }, []);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    let result = lots;
    if (q) {
      result = result.filter(
        (lot) => lot.name.toLowerCase().includes(q) || lot.address.toLowerCase().includes(q)
      );
    }
    if (hideFull) {
      result = result.filter((lot) => lot.availableSlots > 0);
    }
    return [...result].sort((a, b) =>
      sortBy === "name" ? a.name.localeCompare(b.name) : b.availableSlots - a.availableSlots
    );
  }, [lots, query, sortBy, hideFull]);

  function handleSelect(lot: ParkingLotDto) {
    navigate(`/driver/lots/${lot.id}`, { state: { lot } });
  }

  return (
    <PageContainer>
      <PageHeader title="Find Parking" description="Search parking lots by name or location, then book a slot." />

      <div className="mb-6 flex flex-wrap items-center gap-3">
        <div className="relative min-w-[220px] flex-1 sm:max-w-md">
          <Search size={16} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
          <TextInput
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search by lot name or address…"
            className="pl-10"
          />
        </div>
        <Select
          value={sortBy}
          onChange={(e) => setSortBy(e.target.value as SortOption)}
          className="w-auto"
          aria-label="Sort lots"
        >
          <option value="available">Most available</option>
          <option value="name">Name (A–Z)</option>
        </Select>
        <label className="flex items-center gap-2 text-sm text-slate-600">
          <input
            type="checkbox"
            checked={hideFull}
            onChange={(e) => setHideFull(e.target.checked)}
            className="h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500"
          />
          Hide full lots
        </label>
      </div>

      <ErrorMessage>{error}</ErrorMessage>

      {loading ? (
        <SkeletonGrid count={6} />
      ) : filtered.length === 0 ? (
        <Card>
          <EmptyState
            icon={<Search size={22} />}
            title={lots.length === 0 ? "No parking lots available yet" : "No lots match your filters"}
            description={
              lots.length === 0
                ? "Check back once an operator adds one."
                : "Try a different search term, or turn off “Hide full lots”."
            }
          />
        </Card>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {filtered.map((lot) => (
            <LotCard key={lot.id} lot={lot} onSelect={handleSelect} />
          ))}
        </div>
      )}
    </PageContainer>
  );
}
