import type { ParkingSlotDto } from "../types";

// Groups slots into rows the way a seat map does, inferred from the slot number
// convention (e.g. "A1", "A2", "B1" → rows A, B). Slot numbers that don't fit
// that pattern fall into a single "General" row rather than breaking layout.
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

function LegendDot({ className, label }: { className: string; label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 text-xs text-slate-500">
      <span className={`h-3 w-3 rounded-[4px] ${className}`} />
      {label}
    </span>
  );
}

export default function SlotSeatMap({
  slots,
  selectedId = null,
  onSelect,
}: {
  slots: ParkingSlotDto[];
  selectedId?: number | null;
  onSelect?: (slot: ParkingSlotDto) => void;
}) {
  const rows = groupIntoRows(slots);
  const interactive = !!onSelect;

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center gap-4">
        <LegendDot className="border border-emerald-300 bg-white" label="Available" />
        <LegendDot className="bg-slate-200" label="Occupied" />
        {interactive && <LegendDot className="bg-indigo-600" label="Selected" />}
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
            <div className="flex flex-1 gap-2 overflow-x-auto pb-1">
              {row.slots.map((slot) => {
                const available = slot.status === "Available";
                const selected = selectedId === slot.id;
                return (
                  <button
                    key={slot.id}
                    type="button"
                    disabled={!available || !interactive}
                    onClick={() => onSelect?.(slot)}
                    title={`${slot.slotNumber} · ₹${slot.hourlyRate}/hr · ${slot.status}`}
                    className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-lg border text-xs font-bold transition-all ${
                      selected
                        ? "scale-105 border-indigo-600 bg-indigo-600 text-white shadow-md"
                        : available
                          ? `border-emerald-300 bg-white text-emerald-700 ${interactive ? "cursor-pointer hover:border-emerald-500 hover:bg-emerald-50" : ""}`
                          : "cursor-not-allowed border-slate-200 bg-slate-100 text-slate-300"
                    }`}
                  >
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
