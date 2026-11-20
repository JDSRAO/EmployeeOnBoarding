import Link from "next/link";

export default function NotFound() {
  return (
    <div className="page page--narrow">
      <div className="empty-state">
        <span className="eyebrow">404</span>
        <h1>Page not found</h1>
        <p>The page you requested does not exist in this sample application.</p>
        <Link className="button button--primary" href="/">
          Back to directory
        </Link>
      </div>
    </div>
  );
}
