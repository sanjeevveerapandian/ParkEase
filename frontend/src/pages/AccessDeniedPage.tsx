import { Link } from "react-router-dom";
import { ShieldAlert } from "lucide-react";
import { PageContainer, Card, Button } from "../components/ui";

export default function AccessDeniedPage() {
  return (
    <PageContainer>
      <div className="mx-auto max-w-md pt-16 text-center">
        <Card>
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-rose-50 text-rose-500">
            <ShieldAlert size={22} />
          </div>
          <h1 className="mb-2 text-xl font-semibold text-slate-900">Access Denied</h1>
          <p className="mb-6 text-sm text-slate-500">You don't have permission to view this page.</p>
          <Link to="/login">
            <Button variant="secondary">Back to login</Button>
          </Link>
        </Card>
      </div>
    </PageContainer>
  );
}
