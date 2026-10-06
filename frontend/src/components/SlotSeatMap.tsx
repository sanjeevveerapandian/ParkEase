import { Bike, Car, Wrench } from "lucide-react";
import type { ParkingSlotDto, SlotStatus } from "../types";

function groupIntoRows(slots: ParkingSlotDto[]) {
  const rows = new Map<string, { slot: ParkingSlotDto; order: number }[]>();

  for (const slot of slots) {
    const match = slot.slotNumber.match(/^([A-Za-z]+)\s*(\d+)$/);
    const row = match ? match[1].toUpperCase() : "General";
    const order = match ? parseInt(match[2], 10) : Number.MAX_SAFE_INTEGER;
    if (!rows.has(row)) rows.set(row, []);
    rows.get(row)!.push({ slot, order });
  }

  const rowNames = Array.from(rows.keys()).sort((a, b) => {
    if (a === "General") return 1;
    if (b === "General") return -1;
    return a.localeCompare(b);
  });

  return rowNames.map((name) => ({
    name,
    slots: rows
      .get(name)!
      .sort((a, b) => a.order - b.order || a.slot.slotNumber.localeCompare(b.slot.slotNumber))
      .map((entry) => entry.slot),
  }));
}

const statusStyles: Record<SlotStatus, string> = {
  Available: "border-emerald-300 bg-white text-emerald-700",
  Reserved: "border-amber-300 bg-amber-50 text-amber-700",
  Occupied: "border-slate-200 bg-slate-200 text-slate-500",
  Maintenance: "border-slate-300 border-dashed bg-slate-50 text-slate-400",
};

function LegendDot({ className, label }: { className: string; label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 text-xs text-slate-500">
      <span className={`h-3 w-3 rounded-[4px] border ${className}`} />
      {label}
    </span>
  );
}

// Slots as seats. `canSelect` decides which slots are clickable (default: free ones) and
// `dimmed` greys out slots that are free but not usable right now, e.g. a car slot while
// a bike is chosen.
export default function SlotSeatMap({
  slots,
  selectedId = null,
  onSelect,
  canSelect = (s) => s.status === "Available",
  dimmed = () => false,
}: {
  slots: ParkingSlotDto[];
  selectedId?: number | null;
  onSelect?: (slot: ParkingSlotDto) => void;
  canSelect?: (slot: ParkingSlotDto) => boolean;
  dimmed?: (slot: ParkingSlotDto) => boolean;
}) {
  const rows = groupIntoRows(slots);
  const interactive = !!onSelect;

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center gap-x-4 gap-y-2">
        <LegendDot className="border-emerald-300 bg-white" label="Available" />
        <LegendDot className="border-amber-300 bg-amber-50" label="Reserved" />
        <LegendDot className="border-slate-200 bg-slate-200" label="Occupied" />
        <LegendDot className="border-dashed border-slate-300 bg-slate-50" label="Maintenance" />
        {interactive && <LegendDot className="border-indigo-600 bg-indigo-600" label="Selected" />}
        <span className="ml-auto inline-flex items-center gap-3 text-xs text-slate-400">
          <span className="inline-flex items-center gap-1"><Car size={12} /> Car</span>
          <span className="inline-flex items-center gap-1"><Bike size={12} /> Bike</span>
        </span>
      </div>

      <div className="mx-auto mb-8 max-w-md rounded-full bg-slate-100 py-1.5 text-center text-[11px] font-semibold uppercase tracking-widest text-slate-400">
        Entrance
      </div>

      <div className="space-y-3">
        {rows.map((row) => (
          <div key={row.name} className="flex items-center gap-3">
            {row.name !== "General" && (
              <span className="w-5 shrink-0 text-center font-mono text-xs font-semibold text-slate-400">
                {row.name}
              </span>
            )}
            <div className="flex flex-1 gap-2 overflow-x-auto p-1">
              {row.slots.map((slot) => {
                const selected = selectedId === slot.id;
                const selectable = interactive && canSelect(slot);
                const fade = dimmed(slot);
                const TypeIcon = slot.vehicleType === "Bike" ? Bike : Car;
                return (
                  <button
                    key={slot.id}
                    type="button"
                    disabled={!selectable}
                    onClick={() => onSelect?.(slot)}
                    title={`${slot.slotNumber} · ${slot.vehicleType} · ₹${slot.hourlyRate}/hr · ${slot.status}`}
                    className={`relative flex h-14 w-12 shrink-0 flex-col items-center justify-center gap-0.5 rounded-lg border text-xs font-bold transition-all ${
                      selected
                        ? "scale-105 border-indigo-600 bg-indigo-600 text-white shadow-md"
                        : `${statusStyles[slot.status]} ${
                            selectable ? "cursor-pointer hover:border-emerald-500 hover:bg-emerald-50 hover:shadow-sm" : "cursor-default"
                          }`
                    } ${fade && !selected ? "opacity-30" : ""}`}
                  >
                    {slot.status === "Maintenance" ? <Wrench size={12} /> : <TypeIcon size={12} className="opacity-70" />}
                    {slot.slotNumber}
                  </button>
                );
              })}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
