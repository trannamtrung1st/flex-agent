/** Dim protocol ident seated on journey and session instrument rails. */
export function ProtocolPlate({ label, value }: { label: string; value: string }) {
  return (
    <div className="protocol-plate pane pane--dim pane--br">
      <span className="protocol-label">{label}</span>
      <span className="protocol-value">{value}</span>
    </div>
  );
}
