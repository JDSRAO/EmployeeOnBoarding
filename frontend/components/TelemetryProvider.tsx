"use client";

import { useEffect } from "react";
import { initializeTelemetry } from "@/lib/telemetry";

export default function TelemetryProvider() {
  useEffect(() => {
    initializeTelemetry();
  }, []);

  return null;
}
