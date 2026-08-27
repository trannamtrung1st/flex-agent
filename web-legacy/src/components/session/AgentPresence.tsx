interface AgentPresenceProps {
  state: "dormant" | "ready" | "processing";
  activityLabel: string | null;
}

const STATE_LABEL: Record<AgentPresenceProps["state"], string> = {
  dormant: "Dormant",
  ready: "Ready",
  processing: "Processing",
};

export function AgentPresence({ state, activityLabel }: AgentPresenceProps) {
  return (
    <section className="agent-presence" aria-labelledby="agent-presence-heading">
      <h2 id="agent-presence-heading">Agent activity</h2>
      <div className="agent-presence-row">
        <div
          className={`agent-core agent-core-${state}`}
          data-agent-presence={state}
          aria-hidden="true"
        />
        <p>
          <span className="agent-presence-state">{STATE_LABEL[state]}</span>
          {activityLabel ? <span className="agent-presence-activity"> · {activityLabel}</span> : null}
        </p>
      </div>
    </section>
  );
}
