import { Moon, Sun } from "lucide-react";
import { Key } from "../../design-system";
import { useTheme } from "../../hooks/useTheme";

export function ThemeToggle() {
  const { theme, toggleTheme } = useTheme();
  const nextTheme = theme === "dark" ? "light" : "dark";
  const Icon = theme === "dark" ? Sun : Moon;

  return (
    <Key variant="quiet" size="compact" onClick={toggleTheme} ariaLabel={`Switch to ${nextTheme} theme`}>
      <Icon aria-hidden="true" className="icon-sm" focusable="false" />
      {theme === "dark" ? "Light theme" : "Dark theme"}
    </Key>
  );
}
