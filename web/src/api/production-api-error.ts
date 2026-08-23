export class ProductionApiError extends Error {
  readonly status: number;
  readonly outcomeCode?: string;

  constructor(status: number, message: string, outcomeCode?: string) {
    super(message);
    this.name = "ProductionApiError";
    this.status = status;
    this.outcomeCode = outcomeCode;
  }
}
