"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import Avatar from "@/components/Avatar";
import { deleteEmployee, getEmployees, type Employee } from "@/lib/api";
import { departmentTone, formatDate } from "@/lib/format";

export default function EmployeeDirectory() {
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [search, setSearch] = useState("");

  const loadEmployees = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      setEmployees(await getEmployees());
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to load employees.",
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadEmployees();
  }, [loadEmployees]);

  const filteredEmployees = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) {
      return employees;
    }

    return employees.filter((employee) =>
      [employee.name, employee.email, employee.department]
        .join(" ")
        .toLowerCase()
        .includes(query),
    );
  }, [employees, search]);

  const departmentCount = useMemo(
    () => new Set(employees.map((employee) => employee.department)).size,
    [employees],
  );

  async function handleDelete(employee: Employee) {
    const confirmed = window.confirm(
      `Delete ${employee.name}? This removes the employee and their profile picture.`,
    );

    if (!confirmed) {
      return;
    }

    setDeletingId(employee.id);
    setError(null);

    try {
      await deleteEmployee(employee.id);
      setEmployees((current) =>
        current.filter((item) => item.id !== employee.id),
      );
    } catch (exception) {
      setError(
        exception instanceof Error ? exception.message : "Delete failed.",
      );
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <div className="page">
      <section className="hero">
        <div className="hero__content">
          <span className="eyebrow">Azure Fundamentals · Hands-on lab</span>
          <h1>People arrive. The cloud gets them ready.</h1>
          <p>
            An employee onboarding system built with Azure Static Web Apps, App
            Service, SQL Database, Blob Storage and Service Bus.
          </p>
          <div className="hero__actions">
            <Link className="button button--primary" href="/employees/new">
              Add employee
            </Link>
            <Link className="button button--quiet" href="/notifications">
              View notifications
            </Link>
          </div>
        </div>
        <div className="hero__panel" aria-label="System summary">
          <div className="hero__panel-label">Live directory</div>
          <strong>{employees.length}</strong>
          <span>employees across {departmentCount} departments</span>
          <div className="hero__flow">
            <span>API</span>
            <i aria-hidden="true">→</i>
            <span>SQL</span>
            <i aria-hidden="true">→</i>
            <span>Service Bus</span>
            <i aria-hidden="true">→</i>
            <span>Worker</span>
          </div>
        </div>
      </section>

      <section className="section">
        <div className="section__header">
          <div>
            <span className="eyebrow eyebrow--muted">Directory</span>
            <h2>Employees</h2>
          </div>
          <div className="search">
            <label className="sr-only" htmlFor="employee-search">
              Search employees
            </label>
            <input
              id="employee-search"
              type="search"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search name, email or department"
            />
          </div>
        </div>

        {error ? (
          <div className="alert alert--error" role="alert">
            <strong>Could not reach the Employee API.</strong>
            <span>{error}</span>
            <button className="button button--small" onClick={() => void loadEmployees()}>
              Try again
            </button>
          </div>
        ) : null}

        {loading ? (
          <div className="employee-grid" aria-label="Loading employees">
            {[1, 2, 3, 4].map((item) => (
              <div className="employee-card employee-card--skeleton" key={item} />
            ))}
          </div>
        ) : filteredEmployees.length === 0 ? (
          <div className="empty-state">
            <h3>{search ? "No matching employees" : "The directory is empty"}</h3>
            <p>
              {search
                ? "Try a different search term."
                : "Add the first employee to see Blob Storage and Service Bus in action."}
            </p>
            {!search ? (
              <Link className="button button--primary" href="/employees/new">
                Add employee
              </Link>
            ) : null}
          </div>
        ) : (
          <div className="employee-grid">
            {filteredEmployees.map((employee) => (
              <article className="employee-card" key={employee.id}>
                <div className="employee-card__header">
                  <Avatar
                    name={employee.name}
                    imageUrl={employee.profilePictureUrl}
                    size="lg"
                  />
                  <span
                    className={`chip chip--${departmentTone(employee.department)}`}
                  >
                    {employee.department}
                  </span>
                </div>
                <div className="employee-card__body">
                  <h3>{employee.name}</h3>
                  <a href={`mailto:${employee.email}`}>{employee.email}</a>
                  <span className="employee-card__date">
                    Updated {formatDate(employee.updatedAtUtc)}
                  </span>
                </div>
                <div className="employee-card__actions">
                  <Link
                    className="button button--ghost button--small"
                    href={`/employees/edit?id=${employee.id}`}
                  >
                    Edit
                  </Link>
                  <button
                    className="button button--danger button--small"
                    onClick={() => void handleDelete(employee)}
                    disabled={deletingId === employee.id}
                  >
                    {deletingId === employee.id ? "Deleting…" : "Delete"}
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="architecture-strip">
        <div>
          <span>01</span>
          <strong>Static Web Apps</strong>
          <p>Hosts this Next.js frontend on a global static endpoint.</p>
        </div>
        <div>
          <span>02</span>
          <strong>App Service + SQL</strong>
          <p>The API stores employee records through Dapper.</p>
        </div>
        <div>
          <span>03</span>
          <strong>Blob Storage</strong>
          <p>Profile pictures live in blob storage. SQL keeps the URL.</p>
        </div>
        <div>
          <span>04</span>
          <strong>Service Bus + worker</strong>
          <p>Employee events are processed asynchronously into notifications.</p>
        </div>
      </section>
    </div>
  );
}
