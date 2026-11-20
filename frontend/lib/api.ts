export type Employee = {
  id: string;
  name: string;
  email: string;
  department: string;
  profilePictureUrl: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type Notification = {
  id: number;
  eventId: string;
  employeeId: string;
  employeeName: string;
  eventType: string;
  message: string;
  createdAtUtc: string;
};

export type EmployeeInput = {
  name: string;
  email: string;
  department: string;
  profilePicture: File | null;
};

const apiBaseUrl =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5215";

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

type ProblemDetails = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

function toErrorMessage(problem: ProblemDetails | null, status: number): string {
  if (problem?.errors) {
    const messages = Object.values(problem.errors).flat().filter(Boolean);
    if (messages.length > 0) {
      return messages.join(" ");
    }
  }

  return problem?.detail ?? problem?.title ?? `Request failed with status ${status}.`;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    cache: "no-store",
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as ProblemDetails | null;
    throw new ApiError(toErrorMessage(problem, response.status), response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

function toFormData(input: EmployeeInput): FormData {
  const formData = new FormData();
  formData.append("name", input.name);
  formData.append("email", input.email);
  formData.append("department", input.department);

  if (input.profilePicture) {
    formData.append("profilePicture", input.profilePicture);
  }

  return formData;
}

export function getEmployees(): Promise<Employee[]> {
  return request<Employee[]>("/api/employees");
}

export function getEmployee(id: string): Promise<Employee> {
  return request<Employee>(`/api/employees/${id}`);
}

export function createEmployee(input: EmployeeInput): Promise<Employee> {
  return request<Employee>("/api/employees", {
    method: "POST",
    body: toFormData(input),
  });
}

export function updateEmployee(id: string, input: EmployeeInput): Promise<Employee> {
  return request<Employee>(`/api/employees/${id}`, {
    method: "PUT",
    body: toFormData(input),
  });
}

export function deleteEmployee(id: string): Promise<void> {
  return request<void>(`/api/employees/${id}`, { method: "DELETE" });
}

export function getNotifications(take = 25): Promise<Notification[]> {
  return request<Notification[]>(`/api/notifications?take=${take}`);
}
