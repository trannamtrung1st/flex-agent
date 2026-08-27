import { Moon, Sun } from "lucide-react";
import type { Theme } from "../../../lib/theme";
import { Key } from "./Key";

export type { Theme };

export function ThemeToggle({
  theme,
  onToggle,
}: {
  theme: Theme;
  onToggle: () => void;
}) {
  const nextTheme = theme === "dark" ? "light" : "dark";
  const Icon = theme === "dark" ? Sun : Moon;

  return (
    <Key variant="quiet" size="compact" onClick={onToggle} ariaLabel={`Switch to ${nextTheme} theme`}>
      <Icon aria-hidden="true" className="icon-sm" focusable="false" />
      <span className="key-label">{theme === "dark" ? "Light theme" : "Dark theme"}</span>
    </Key>
  );
}
