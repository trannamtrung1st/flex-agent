import { ThemeToggle } from "../../../design-system/components/keys";
import { useTheme } from "../../../lib/useTheme";

export function LabThemeToggle() {
  const { theme, toggleTheme } = useTheme();
  return <ThemeToggle theme={theme} onToggle={toggleTheme} />;
}
