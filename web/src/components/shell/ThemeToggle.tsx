import { Moon, Sun } from "lucide-react";
import { useTheme } from "../../hooks/useTheme";
import { Button } from "../ui/Button";

export function ThemeToggle() {
  const { theme, toggleTheme } = useTheme();
  const nextTheme = theme === "dark" ? "light" : "dark";
  const Icon = theme === "dark" ? Sun : Moon;

  return (
    <Button
      variant="ghost"
      size="sm"
      onClick={toggleTheme}
      aria-label={`Switch to ${nextTheme} theme`}
    >
      <Icon aria-hidden="true" className="icon-sm" focusable="false" />
      {theme === "dark" ? "Light theme" : "Dark theme"}
    </Button>
  );
}
