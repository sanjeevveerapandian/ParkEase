import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { Link } from "react-router-dom";
import { Car, Plus, Trash2 } from "lucide-react";
import { vehiclesApi } from "../../api/endpoints";
import { getErrorMessage } from "../../api/errors";
import { useToast } from "../../context/ToastContext";
import { VehicleIcon } from "../../components/BookingParts";
import {
  Button,
  Card,
  EmptyState,
  ErrorMessage,
  Field,
  PageContainer,
  PageHeader,
  Select,
  Skeleton,
  TextInput,
} from "../../components/ui";
import type { VehicleDto, VehicleType } from "../../types";

const PLATE_PATTERN = /^[A-Z]{2}\d{1,2}[A-Z]{1,3}\d{4}$/;

export default function MyVehiclesPage() {
  const toast = useToast();
  const [vehicles, setVehicles] = useState<VehicleDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [plate, setPlate] = useState("");
  const [type, setType] = useState<VehicleType>("Car");
  const [addError, setAddError] = useState("");
  const [adding, setAdding] = useState(false);
  const [removingId, setRemovingId] = useState<number | null>(null);

  async function load() {
    setError("");
    try {
      const res = await vehiclesApi.mine();
      setVehicles(res.data);
    } catch (err) {
      setError(getErrorMessage(err, "Could not load your vehicles."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  async function handleAdd(e: FormEvent) {
    e.preventDefault();
    setAddError("");
    const normalized = plate.replace(/[\s-]/g, "").toUpperCase();
    if (!PLATE_PATTERN.test(normalized)) {
      setAddError("Enter a valid vehicle number, e.g. TN09AB1234.");
      return;
    }
    setAdding(true);
    try {
      await vehiclesApi.add({ vehicleNumber: normalized, type });
      toast.success(`Added ${normalized}.`);
      setPlate("");
      await load();
    } catch (err) {
      setAddError(getErrorMessage(err, "Could not add vehicle."));
    } finally {
      setAdding(false);
    }
  }

  async function handleRemove(v: VehicleDto) {
    setRemovingId(v.id);
    try {
      await vehiclesApi.remove(v.id);
      toast.success(`Removed ${v.vehicleNumber}.`);
      await load();
    } catch (err) {
      toast.error(getErrorMessage(err, "Could not remove vehicle."));
    } finally {
      setRemovingId(null);
    }
  }

  return (
    <PageContainer>
      <PageHeader
        title="My Vehicles"
        description="Each booking maps one of these vehicles to one slot. Cars use car slots, bikes use bike slots."
      />
      <ErrorMessage>{error}</ErrorMessage>

      <Card className="mb-6">
        <h2 className="mb-3 flex items-center gap-1.5 text-base font-semibold text-slate-900">
          <Plus size={16} /> Add a vehicle
        </h2>
        <form onSubmit={handleAdd} className="flex flex-wrap items-end gap-3">
          <div className="min-w-[200px] flex-1">
            <Field label="Vehicle number" htmlFor="plate">
              <TextInput
                id="plate"
                required
                value={plate}
                onChange={(e) => setPlate(e.target.value.toUpperCase())}
                placeholder="TN09AB1234"
                className="uppercase placeholder:normal-case"
              />
            </Field>
          </div>
          <div className="min-w-[140px]">
            <Field label="Type" htmlFor="vtype">
              <Select id="vtype" value={type} onChange={(e) => setType(e.target.value as VehicleType)}>
                <option value="Car">Car</option>
                <option value="Bike">Bike</option>
              </Select>
            </Field>
          </div>
          <Button type="submit" loading={adding} className="mb-4">
            {adding ? "Adding…" : "Add vehicle"}
          </Button>
        </form>
        <ErrorMessage>{addError}</ErrorMessage>
      </Card>

      <Card className="!p-0 overflow-hidden">
        {loading ? (
          <div className="space-y-3 p-6">
            {Array.from({ length: 2 }).map((_, i) => (
              <Skeleton key={i} className="h-14" />
            ))}
          </div>
        ) : vehicles.length === 0 ? (
          <EmptyState
            icon={<Car size={22} />}
            title="No vehicles yet"
            description="Add the vehicle you'll park, then pick a slot for it."
          />
        ) : (
          <ul className="divide-y divide-slate-100">
            {vehicles.map((v) => (
              <li key={v.id} className="flex items-center justify-between gap-4 px-6 py-4">
                <div className="flex items-center gap-3">
                  <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600">
                    <VehicleIcon type={v.type} size={18} />
                  </span>
                  <div>
                    <p className="font-semibold tracking-wide text-slate-900">{v.vehicleNumber}</p>
                    <p className="text-xs text-slate-500">{v.type}</p>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <Link
                    to="/driver/lots"
                    className="rounded-lg px-3 py-2 text-sm font-medium text-indigo-600 hover:bg-indigo-50"
                  >
                    Find a slot
                  </Link>
                  <Button
                    variant="ghost"
                    icon={<Trash2 size={14} />}
                    loading={removingId === v.id}
                    onClick={() => handleRemove(v)}
                    aria-label={`Remove ${v.vehicleNumber}`}
                  >
                    Remove
                  </Button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </PageContainer>
  );
}
