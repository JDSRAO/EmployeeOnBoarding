"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense } from "react";
import EmployeeForm from "@/components/EmployeeForm";

function EditEmployeeContent() {
  const searchParams = useSearchParams();
  const employeeId = searchParams.get("id");

  if (!employeeId) {
    return (
      <div className="page page--narrow">
        <div className="empty-state">
          <span className="eyebrow">Edit employee</span>
          <h1>Select an employee first</h1>
          <p>
            Open this page from the employee directory so the employee ID can be
            loaded.
          </p>
          <Link className="button button--primary" href="/">
            Back to directory
          </Link>
        </div>
      </div>
    );
  }

  return <EmployeeForm employeeId={employeeId} />;
}

export default function EditEmployeePage() {
  return (
    <Suspense
      fallback={
        <div className="page page--narrow">
          <div className="form-card form-card--skeleton" />
        </div>
      }
    >
      <EditEmployeeContent />
    </Suspense>
  );
}
