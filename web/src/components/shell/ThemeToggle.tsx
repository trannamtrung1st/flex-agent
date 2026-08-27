import { Moon, Sun } from "lucide-react";
import { Key } from "../../design-system";
import { useTheme, type Theme } from "../../hooks/useTheme";

export function ThemeToggle({
  theme,
  onToggle,
}: {
  theme?: Theme;
  onToggle?: () => void;
}) {
  const local = useTheme();
  const resolved = theme ?? local.theme;
  const toggle = onToggle ?? local.toggleTheme;
  const nextTheme = resolved === "dark" ? "light" : "dark";
  const Icon = resolved === "dark" ? Sun : Moon;

  return (
    <Key variant="quiet" size="compact" onClick={toggle} ariaLabel={`Switch to ${nextTheme} theme`}>
      <Icon aria-hidden="true" className="icon-sm" focusable="false" />
      {resolved === "dark" ? "Light theme" : "Dark theme"}
    </Key>
  );
}
