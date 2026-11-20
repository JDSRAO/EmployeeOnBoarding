import { initials } from "@/lib/format";

type AvatarProps = {
  name: string;
  imageUrl?: string | null;
  size?: "sm" | "md" | "lg";
};

export default function Avatar({ name, imageUrl, size = "md" }: AvatarProps) {
  return (
    <span className={`avatar avatar--${size}`}>
      {imageUrl ? (
        <img src={imageUrl} alt={`${name} profile picture`} />
      ) : (
        <span aria-hidden="true">{initials(name)}</span>
      )}
    </span>
  );
}
