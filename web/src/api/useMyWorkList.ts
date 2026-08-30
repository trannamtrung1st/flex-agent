import { useCallback, useEffect, useMemo, useState } from "react";
import { useProductionApi } from "./production-api";
import {
  createProductionEnrollmentClient,
  enrollmentFailureCopy,
  type AssignmentSummaryV1,
} from "./production-enrollment";

export function useMyWorkList(enabled: boolean) {
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const [items, setItems] = useState<AssignmentSummaryV1[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  const load = useCallback((signal?: { cancelled: boolean }) => {
    if (!enabled) {
      return Promise.resolve();
    }
    return client.listMyWork()
      .then((page) => {
        if (signal?.cancelled) return;
        setItems(page.items);
        setError(null);
      })
      .catch((caught: unknown) => {
        if (!signal?.cancelled) {
          setError(enrollmentFailureCopy(caught, "My work is not available."));
        }
      });
  }, [client, enabled]);

  useEffect(() => {
    if (!enabled) {
      setItems(null);
      setError(null);
      return;
    }
    const signal = { cancelled: false };
    void load(signal);
    return () => {
      signal.cancelled = true;
    };
  }, [enabled, load]);

  return { items, error, pending, setPending, load };
}
