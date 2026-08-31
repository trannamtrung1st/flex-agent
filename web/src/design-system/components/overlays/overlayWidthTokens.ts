export function rewriteOverlayPercent(value: string, percentBasePx: number): string {
  return value.replace(/(-?\d+(?:\.\d+)?)%/g, (_, amount: string) => `${(Number(amount) / 100) * percentBasePx}px`);
}

function splitArgs(inner: string): string[] {
  const parts: string[] = [];
  let depth = 0;
  let current = "";
  for (const ch of inner) {
    if (ch === "(") depth += 1;
    else if (ch === ")") depth -= 1;
    if (ch === "," && depth === 0) {
      parts.push(current.trim());
      current = "";
      continue;
    }
    current += ch;
  }
  if (current.trim()) parts.push(current.trim());
  return parts;
}

function lengthToPx(
  amount: number,
  unit: string,
  ctx: { viewportWidth: number; viewportHeight: number; rootFontPx: number },
): number {
  if (unit === "rem" || unit === "em") return amount * ctx.rootFontPx;
  if (unit === "vw") return (amount / 100) * ctx.viewportWidth;
  if (unit === "vh") return (amount / 100) * ctx.viewportHeight;
  return amount;
}

function evalCalc(
  expr: string,
  ctx: { percentBasePx: number; viewportWidth: number; viewportHeight: number; rootFontPx: number },
): number | undefined {
  const match = expr.trim().match(/^(-?\d+(?:\.\d+)?)(px|rem|em|vw|vh)\s*([+-])\s*(-?\d+(?:\.\d+)?)(px|rem|em|vw|vh)$/i);
  if (!match) return overlayTokenToPx(expr, ctx);
  const left = lengthToPx(Number(match[1]), match[2].toLowerCase(), ctx);
  const right = lengthToPx(Number(match[4]), match[5].toLowerCase(), ctx);
  return match[3] === "-" ? left - right : left + right;
}

export function overlayTokenToPx(
  value: string,
  ctx: { percentBasePx: number; viewportWidth: number; viewportHeight?: number; rootFontPx?: number },
): number | undefined {
  const viewportHeight = ctx.viewportHeight ?? ctx.viewportWidth;
  const rootFontPx = ctx.rootFontPx ?? 16;
  const raw = rewriteOverlayPercent(value.trim(), ctx.percentBasePx);
  if (!raw || raw === "none" || raw === "auto") return undefined;

  const minMax = raw.match(/^(min|max)\((.+)\)$/i);
  if (minMax) {
    const nums = splitArgs(minMax[2])
      .map((part) => overlayTokenToPx(part, { ...ctx, viewportHeight, rootFontPx }))
      .filter((n): n is number => n != null);
    if (!nums.length) return undefined;
    return minMax[1].toLowerCase() === "min" ? Math.min(...nums) : Math.max(...nums);
  }

  const calc = raw.match(/^calc\((.+)\)$/i);
  if (calc) return evalCalc(calc[1], { ...ctx, viewportHeight, rootFontPx });

  const length = raw.match(/^(-?\d+(?:\.\d+)?)(px|rem|em|vw|vh)$/i);
  if (!length) return undefined;
  return lengthToPx(Number(length[1]), length[2].toLowerCase(), { viewportWidth: ctx.viewportWidth, viewportHeight, rootFontPx });
}

export function overlayBoxWidth({
  triggerWidth,
  viewportWidth,
  minWidthToken,
  maxWidthToken,
  stretch,
  lockMinWidthToTrigger,
  rootFontPx,
}: {
  triggerWidth: number;
  viewportWidth: number;
  minWidthToken: string;
  maxWidthToken: string;
  stretch: boolean;
  lockMinWidthToTrigger: boolean;
  rootFontPx?: number;
}): { width?: number; minWidth?: number; maxWidth: number } {
  const ctx = { percentBasePx: triggerWidth, viewportWidth, rootFontPx: rootFontPx ?? 16 };
  const tokenMin = overlayTokenToPx(minWidthToken, ctx) ?? 0;
  const tokenMax = overlayTokenToPx(maxWidthToken, ctx);
  if (!stretch && !lockMinWidthToTrigger) {
    return { maxWidth: Math.min(viewportWidth, tokenMax ?? viewportWidth) };
  }
  const minWidth = Math.min(viewportWidth, Math.max(triggerWidth, tokenMin));
  const maxWidth = Math.min(viewportWidth, Math.max(minWidth, tokenMax ?? viewportWidth));
  if (stretch) return { width: minWidth, minWidth, maxWidth };
  return { minWidth, maxWidth };
}
