export function initials(name: string): string {
  return name
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");
}

export function formatDate(value: string): string {
  return new Intl.DateTimeFormat("en", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function departmentTone(department: string): string {
  const tones = ["sky", "violet", "emerald", "amber", "rose"];
  const hash = Array.from(department).reduce(
    (total, character) => total + character.charCodeAt(0),
    0,
  );

  return tones[hash % tones.length];
}
