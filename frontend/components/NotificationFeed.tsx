"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { getNotifications, type Notification } from "@/lib/api";
import { formatDate } from "@/lib/format";

export default function NotificationFeed() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const loadNotifications = useCallback(async (showSpinner = false) => {
    if (showSpinner) {
      setLoading(true);
    }

    try {
      const result = await getNotifications(50);
      setNotifications(result);
      setLastUpdated(new Date());
      setError(null);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to load notifications.",
      );
    } finally {
      if (showSpinner) {
        setLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    void loadNotifications(true);
    const timer = window.setInterval(() => void loadNotifications(), 5000);
    return () => window.clearInterval(timer);
  }, [loadNotifications]);

  return (
    <div className="page page--narrow">
      <nav className="breadcrumb" aria-label="Breadcrumb">
        <Link href="/">Directory</Link>
        <span aria-hidden="true">/</span>
        <span>Notifications</span>
      </nav>

      <div className="feed-heading">
        <div>
          <span className="eyebrow">Asynchronous processing</span>
          <h1>Onboarding notifications</h1>
          <p>
            The API publishes employee events to Azure Service Bus. A background
            worker consumes them and writes notification records to SQL.
          </p>
        </div>
        <span className="live-badge">
          <i aria-hidden="true" />
          Live
        </span>
      </div>

      <div className="callout">
        <strong>Why a queue?</strong>
        <span>
          Adding an employee stays fast because notification creation is
          decoupled. Service Bus buffers the event and the worker retries
          processing if something goes wrong.
        </span>
      </div>

      {error ? (
        <div className="alert alert--error" role="alert">
          {error}
        </div>
      ) : null}

      <div className="feed">
        <div className="feed__meta">
          <span>{notifications.length} notifications</span>
          <span>
            {lastUpdated
              ? `Updated ${lastUpdated.toLocaleTimeString()}`
              : "Connecting…"}
          </span>
        </div>

        {loading ? (
          <div className="feed__empty">Loading notifications…</div>
        ) : notifications.length === 0 ? (
          <div className="feed__empty">
            <h2>No notifications yet</h2>
            <p>
              Add or update an employee. The worker will pick up the event and a
              notification will appear here within a few seconds.
            </p>
            <Link className="button button--primary" href="/employees/new">
              Add employee
            </Link>
          </div>
        ) : (
          <ol className="timeline">
            {notifications.map((notification) => (
              <li className="timeline__item" key={notification.id}>
                <span
                  className={
                    notification.eventType === "Created"
                      ? "timeline__dot timeline__dot--created"
                      : "timeline__dot timeline__dot--updated"
                  }
                  aria-hidden="true"
                />
                <div className="timeline__content">
                  <div className="timeline__top">
                    <span
                      className={
                        notification.eventType === "Created"
                          ? "event-chip event-chip--created"
                          : "event-chip event-chip--updated"
                      }
                    >
                      {notification.eventType}
                    </span>
                    <time dateTime={notification.createdAtUtc}>
                      {formatDate(notification.createdAtUtc)}
                    </time>
                  </div>
                  <strong>{notification.message}</strong>
                  <span className="timeline__id">
                    Event {notification.eventId.slice(0, 8)} · Employee{" "}
                    {notification.employeeId.slice(0, 8)}
                  </span>
                </div>
              </li>
            ))}
          </ol>
        )}
      </div>
    </div>
  );
}
