import { useTheme, type Theme } from "../../hooks/useTheme";
import { ThemeToggle as PresentationalThemeToggle } from "../../design-system";

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

  return <PresentationalThemeToggle theme={resolved} onToggle={toggle} />;
}
