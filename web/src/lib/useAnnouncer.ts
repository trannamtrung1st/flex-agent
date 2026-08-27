import { useCallback, useState } from "react";

export function useAnnouncer() {
  const [message, setMessage] = useState("");

  const announce = useCallback((next: string) => {
    setMessage("");
    window.setTimeout(() => setMessage(next), 30);
  }, []);

  return { message, announce };
}

