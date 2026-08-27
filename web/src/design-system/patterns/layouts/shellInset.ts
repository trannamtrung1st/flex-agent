import type { LayoutSpace } from "../../components/layout/types";

/** Matches command-strip brand inline inset (`--space-5-5` / 22px). */
export const SHELL_MAIN_INSET_INLINE = "5.5" satisfies LayoutSpace;

/** Workspace vertical hull pad (`--space-4` / 16px). */
export const SHELL_MAIN_INSET_BLOCK = "4" satisfies LayoutSpace;

/** CSS modifier: hull pad reads `--shell-main-inset-*` tokens (responsive). */
export const SHELL_MAIN_INSET_CLASS = "composition-inset--shell-main";
