import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { Link } from "react-router-dom";
import { Building2, MapPin, Plus, Trash2, ArrowRight, ParkingSquare } from "lucide-react";
import { parkingLotsApi } from "../../api/endpoints";
import { getErrorMessage } from "../../api/errors";
import { useAuth } from "../../context/AuthContext";
import { useToast } from "../../context/ToastContext";
import {
  Button,
  Card,
  EmptyState,
  ErrorMessage,
  Field,
  Modal,
  PageContainer,
  PageHeader,
  SkeletonGrid,
  TextInput,
} from "../../components/ui";
import type { ParkingLotDto } from "../../types";

function LotCard({ lot, onDelete }: { lot: ParkingLotDto; onDelete: (lot: ParkingLotDto) => void }) {
  const occupied = lot.totalSlots - lot.availableSlots;
  const pct = lot.totalSlots > 0 ? Math.round((occupied / lot.totalSlots) * 100) : 0;

  return (
    <Card className="flex flex-col">
      <div className="flex items-start justify-between">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600">
          <Building2 size={18} />
        </div>
        <button
          onClick={() => onDelete(lot)}
          className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-rose-50 hover:text-rose-600"
          aria-label={`Delete ${lot.name}`}
        >
          <Trash2 size={16} />
        </button>
      </div>

      <Link to={`/operator/lots/${lot.id}`} className="mt-3 text-base font-semibold text-slate-900 hover:text-indigo-600">
        {lot.name}
      </Link>
      <p className="mt-1 flex items-center gap-1 text-sm text-slate-500">
        <MapPin size={13} className="shrink-0" />
        <span className="truncate">{lot.address}</span>
      </p>

      <div className="mt-4">
        <div className="flex items-center justify-between text-xs text-slate-500">
          <span>
            {lot.availableSlots} of {lot.totalSlots} available
          </span>
          <span>{pct}% occupied</span>
        </div>
        <div className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-slate-100">
          <div
            className={`h-full rounded-full ${pct >= 80 ? "bg-rose-500" : pct >= 40 ? "bg-amber-500" : "bg-emerald-500"}`}
            style={{ width: `${pct}%` }}
          />
        </div>
      </div>

      <Link to={`/operator/lots/${lot.id}`} className="mt-4">
        <Button variant="secondary" className="w-full" icon={<ArrowRight size={14} />}>
          Manage
        </Button>
      </Link>
    </Card>
  );
}

export default function LotsPage() {
  const { role } = useAuth();
  const toast = useToast();
  const [lots, setLots] = useState<ParkingLotDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [name, setName] = useState("");
  const [address, setAddress] = useState("");
  const [createError, setCreateError] = useState("");
  const [creating, setCreating] = useState(false);

  const [deleteTarget, setDeleteTarget] = useState<ParkingLotDto | null>(null);
  const [deleting, setDeleting] = useState(false);

  async function loadLots() {
    setLoading(true);
    setError("");
    try {
      const res = await parkingLotsApi.list();
      setLots(res.data);
    } catch (err) {
      setError(getErrorMessage(err, "Could not load parking lots."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadLots();
  }, []);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setCreateError("");
    setCreating(true);
    try {
      await parkingLotsApi.create({ name, address });
      setName("");
      setAddress("");
      toast.success(`Created lot "${name}".`);
      await loadLots();
    } catch (err) {
      setCreateError(getErrorMessage(err, "Could not create lot."));
    } finally {
      setCreating(false);
    }
  }

  async function handleConfirmDelete() {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await parkingLotsApi.remove(deleteTarget.id);
      toast.success(`Deleted lot "${deleteTarget.name}".`);
      setDeleteTarget(null);
      await loadLots();
    } catch (err) {
      toast.error(getErrorMessage(err, "Could not delete lot."));
    } finally {
      setDeleting(false);
    }
  }

  return (
    <PageContainer>
      <PageHeader
        title="Parking Lots"
        description={role === "Admin" ? "All lots across every operator." : "Lots you own and manage."}
      />

      {(role === "Operator" || role === "Admin") && (
        <Card className="mb-6">
          <h2 className="mb-3 flex items-center gap-1.5 text-base font-semibold text-slate-900">
            <Plus size={16} /> Create a new lot
          </h2>
          <form onSubmit={handleCreate} className="flex flex-wrap items-end gap-3">
            <div className="min-w-[200px] flex-1">
              <Field label="Name" htmlFor="name">
                <TextInput
                  id="name"
                  required
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="Anna Nagar Lot 1"
                />
              </Field>
            </div>
            <div className="min-w-[240px] flex-1">
              <Field label="Address" htmlFor="address">
                <TextInput
                  id="address"
                  required
                  value={address}
                  onChange={(e) => setAddress(e.target.value)}
                  placeholder="12 2nd Ave, Anna Nagar, Chennai"
                />
              </Field>
            </div>
            <Button type="submit" loading={creating} className="mb-4">
              {creating ? "Creating…" : "Create Lot"}
            </Button>
          </form>
          <ErrorMessage>{createError}</ErrorMessage>
        </Card>
      )}

      <ErrorMessage>{error}</ErrorMessage>

      <Modal open={!!deleteTarget} onClose={() => setDeleteTarget(null)} title="Delete parking lot">
        <p className="text-sm text-slate-600">
          Are you sure you want to delete <span className="font-medium text-slate-900">{deleteTarget?.name}</span>?
          This can't be undone.
        </p>
        <div className="mt-5 flex gap-2">
          <Button variant="danger" loading={deleting} onClick={handleConfirmDelete} className="flex-1">
            {deleting ? "Deleting…" : "Delete lot"}
          </Button>
          <Button variant="secondary" onClick={() => setDeleteTarget(null)}>
            Cancel
          </Button>
        </div>
      </Modal>

      {loading ? (
        <SkeletonGrid count={6} />
      ) : lots.length === 0 ? (
        <Card>
          <EmptyState
            icon={<ParkingSquare size={22} />}
            title="No parking lots yet"
            description={
              role === "Operator" || role === "Admin"
                ? "Create your first lot above to get started."
                : "No lots have been created yet."
            }
          />
        </Card>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {lots.map((lot) => (
            <LotCard key={lot.id} lot={lot} onDelete={setDeleteTarget} />
          ))}
        </div>
      )}
    </PageContainer>
  );
}
