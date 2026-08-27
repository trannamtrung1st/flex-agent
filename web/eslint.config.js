import js from "@eslint/js";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import globals from "globals";
import tseslint from "typescript-eslint";

export default tseslint.config(
  { ignores: ["dist", "dist-design-lab", "test-results", "playwright-report", "e2e", "playwright.design-lab.config.ts"] },
  {
    files: ["src/**/*.{ts,tsx}"],
    ignores: ["src/design-lab/**"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: ["**/design-lab", "**/design-lab/**"],
              message: "Candidate and design-system modules must not import the design lab.",
            },
            {
              group: ["**/web-legacy", "**/web-legacy/**"],
              message: "Candidate modules must not import web-legacy.",
            },
          ],
        },
      ],
    },
  },
  {
    extends: [js.configs.recommended, ...tseslint.configs.strictTypeChecked],
    files: ["**/*.{ts,tsx}"],
    ignores: ["src/design-lab/**"],
    languageOptions: {
      ecmaVersion: 2022,
      globals: globals.browser,
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: {
      "react-hooks": reactHooks,
      "react-refresh": reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      "react-refresh/only-export-components": [
        "warn",
        { allowConstantExport: true },
      ],
    },
  },
  {
    extends: [js.configs.recommended, ...tseslint.configs.strictTypeChecked],
    files: ["src/design-lab/**/*.{ts,tsx}"],
    languageOptions: {
      ecmaVersion: 2022,
      globals: globals.browser,
      parserOptions: {
        project: "./tsconfig.design-lab.json",
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: {
      "react-hooks": reactHooks,
      "react-refresh": reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      "react-refresh/only-export-components": [
        "warn",
        { allowConstantExport: true },
      ],
    },
  },
  {
    files: [
      "src/lib/**/*.{ts,tsx}",
      "src/design-system/**/*.{ts,tsx}",
      "src/design-lab/components/**/*.{ts,tsx}",
      "src/design-lab/lib/**/*.{ts,tsx}",
      "src/design-lab/data/**/*.{ts,tsx}",
      "src/design-lab/routes/**/*.{ts,tsx}",
      "src/design-lab/features/**/*.{ts,tsx}",
      "src/design-lab/app/**/*.{ts,tsx}",
    ],
    rules: {
      "@typescript-eslint/no-confusing-void-expression": "off",
      "@typescript-eslint/restrict-template-expressions": "off",
      "@typescript-eslint/no-unnecessary-condition": "off",
      "@typescript-eslint/no-floating-promises": "off",
      "@typescript-eslint/no-non-null-assertion": "off",
      "@typescript-eslint/no-misused-promises": "off",
      "@typescript-eslint/no-unnecessary-type-assertion": "off",
      "react-refresh/only-export-components": "off",
      "react-hooks/exhaustive-deps": "off",
      "react-hooks/incompatible-library": "off",
    },
  },
);
