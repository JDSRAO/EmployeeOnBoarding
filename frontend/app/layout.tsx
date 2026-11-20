import type { Metadata } from "next";
import Link from "next/link";
import TelemetryProvider from "@/components/TelemetryProvider";
import "./globals.css";

export const metadata: Metadata = {
  title: "Onboard · Employee Onboarding",
  description:
    "Azure Fundamentals sample application for employee onboarding, built with Next.js and Azure.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>
        <div className="app-shell">
          <header className="site-header">
            <Link className="brand" href="/">
              <span className="brand__mark" aria-hidden="true">
                O
              </span>
              <span>
                Onboard
                <small>Azure Fundamentals</small>
              </span>
            </Link>
            <nav className="site-nav" aria-label="Main navigation">
              <Link href="/">Directory</Link>
              <Link href="/notifications">Notifications</Link>
              <Link className="site-nav__cta" href="/employees/new">
                Add employee
              </Link>
            </nav>
          </header>
          <main>{children}</main>
          <footer className="site-footer">
            <span>Employee Onboarding · Azure Fundamentals training sample</span>
            <span>Static Web Apps · App Service · SQL · Blob · Service Bus</span>
          </footer>
        </div>
        <TelemetryProvider />
      </body>
    </html>
  );
}
