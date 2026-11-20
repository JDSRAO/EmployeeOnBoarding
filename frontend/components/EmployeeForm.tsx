"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import Avatar from "@/components/Avatar";
import {
  createEmployee,
  getEmployee,
  updateEmployee,
  type EmployeeInput,
} from "@/lib/api";

type EmployeeFormProps = {
  employeeId?: string;
};

const departments = [
  "Engineering",
  "Finance",
  "Human Resources",
  "Marketing",
  "People Operations",
  "Sales",
  "Customer Success",
];

const maxFileSize = 5 * 1024 * 1024;

export default function EmployeeForm({ employeeId }: EmployeeFormProps) {
  const router = useRouter();
  const isEditing = Boolean(employeeId);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [department, setDepartment] = useState("Engineering");
  const [profilePicture, setProfilePicture] = useState<File | null>(null);
  const [currentPictureUrl, setCurrentPictureUrl] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(isEditing);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!employeeId) {
      return;
    }

    let cancelled = false;

    async function loadEmployee() {
      try {
        const employee = await getEmployee(employeeId as string);
        if (cancelled) {
          return;
        }

        setName(employee.name);
        setEmail(employee.email);
        setDepartment(employee.department);
        setCurrentPictureUrl(employee.profilePictureUrl);
      } catch (exception) {
        if (!cancelled) {
          setError(
            exception instanceof Error
              ? exception.message
              : "Unable to load employee.",
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void loadEmployee();

    return () => {
      cancelled = true;
    };
  }, [employeeId]);

  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  function handleFileChange(file: File | null) {
    setError(null);

    if (!file) {
      setProfilePicture(null);
      setPreviewUrl(null);
      return;
    }

    if (file.size > maxFileSize) {
      setError("The profile picture must be 5 MB or smaller.");
      return;
    }

    if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
      setError("Choose a JPEG, PNG or WebP image.");
      return;
    }

    setProfilePicture(file);
    setPreviewUrl(URL.createObjectURL(file));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (!name.trim() || !email.trim() || !department.trim()) {
      setError("Name, email and department are required.");
      return;
    }

    const input: EmployeeInput = {
      name: name.trim(),
      email: email.trim(),
      department: department.trim(),
      profilePicture,
    };

    setSubmitting(true);

    try {
      if (employeeId) {
        await updateEmployee(employeeId, input);
      } else {
        await createEmployee(input);
      }

      router.push("/");
      router.refresh();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "The employee could not be saved.",
      );
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return (
      <div className="page page--narrow">
        <div className="form-card form-card--skeleton" />
      </div>
    );
  }

  return (
    <div className="page page--narrow">
      <nav className="breadcrumb" aria-label="Breadcrumb">
        <Link href="/">Directory</Link>
        <span aria-hidden="true">/</span>
        <span>{isEditing ? "Edit employee" : "Add employee"}</span>
      </nav>

      <div className="form-heading">
        <span className="eyebrow">People Operations</span>
        <h1>{isEditing ? "Edit employee" : "Add a new employee"}</h1>
        <p>
          The API stores employee details in Azure SQL Database and uploads the
          profile picture to Azure Blob Storage.
        </p>
      </div>

      {error ? (
        <div className="alert alert--error" role="alert">
          {error}
        </div>
      ) : null}

      <form className="form-card" onSubmit={handleSubmit}>
        <div className="form-grid">
          <div className="form-fields">
            <label className="field">
              <span>Full name</span>
              <input
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="Alex Johnson"
                maxLength={150}
                required
              />
            </label>

            <label className="field">
              <span>Work email</span>
              <input
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="alex.johnson@example.com"
                maxLength={255}
                required
              />
            </label>

            <label className="field">
              <span>Department</span>
              <input
                value={department}
                onChange={(event) => setDepartment(event.target.value)}
                list="departments"
                maxLength={100}
                required
              />
              <datalist id="departments">
                {departments.map((option) => (
                  <option value={option} key={option} />
                ))}
              </datalist>
            </label>
          </div>

          <aside className="photo-field">
            <span className="photo-field__label">Profile picture</span>
            <Avatar
              name={name || "New employee"}
              imageUrl={previewUrl ?? currentPictureUrl}
              size="lg"
            />
            <label className="button button--ghost button--small">
              {profilePicture ? "Choose another" : "Upload image"}
              <input
                type="file"
                accept="image/jpeg,image/png,image/webp"
                onChange={(event) =>
                  handleFileChange(event.target.files?.[0] ?? null)
                }
                hidden
              />
            </label>
            <span className="photo-field__hint">
              {profilePicture
                ? profilePicture.name
                : "JPEG, PNG or WebP. Maximum 5 MB."}
            </span>
            {profilePicture ? (
              <button
                type="button"
                className="text-button"
                onClick={() => handleFileChange(null)}
              >
                Remove selected image
              </button>
            ) : null}
          </aside>
        </div>

        <div className="callout">
          <strong>Where does the image go?</strong>
          <span>
            The API streams the file to the profile-pictures container in Azure
            Blob Storage. Only the resulting URL is written to SQL Database.
          </span>
        </div>

        <div className="form-actions">
          <Link className="button button--quiet" href="/">
            Cancel
          </Link>
          <button
            className="button button--primary"
            type="submit"
            disabled={submitting}
          >
            {submitting
              ? "Saving…"
              : isEditing
                ? "Save changes"
                : "Add employee"}
          </button>
        </div>
      </form>
    </div>
  );
}
