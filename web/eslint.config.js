import js from "@eslint/js";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import globals from "globals";
import tseslint from "typescript-eslint";

export default tseslint.config(
  {
    ignores: [
      "dist",
      "dist-design-lab",
      "test-results",
      "playwright-report",
    ],
  },
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
            {
              group: [
                "**/styles/design-lab.css",
                "**/styles/components/demo.css",
                "**/styles/surfaces/**",
              ],
              message: "Candidate modules must load lab-only styles through the design lab entry graph.",
            },
          ],
        },
      ],
    },
  },
  {
    extends: [js.configs.recommended, ...tseslint.configs.strictTypeChecked],
    files: ["**/*.{ts,tsx}"],
    ignores: [
      "src/design-lab/**",
      "e2e/design-lab/**",
      "vite.design-lab.config.ts",
      "vitest.design-lab.config.ts",
      "playwright.design-lab.config.ts",
    ],
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
    files: [
      "src/design-lab/**/*.{ts,tsx}",
      "e2e/design-lab/**/*.{ts,tsx}",
      "playwright.design-lab.config.ts",
      "vite.design-lab.config.ts",
      "vitest.design-lab.config.ts",
    ],
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
    files: ["**/*.{test,spec}.{ts,tsx}"],
    rules: {
      "@typescript-eslint/no-unsafe-return": "off",
      "@typescript-eslint/no-unsafe-call": "off",
      "@typescript-eslint/no-unsafe-member-access": "off",
      "@typescript-eslint/no-unsafe-assignment": "off",
      "@typescript-eslint/restrict-template-expressions": "off",
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
