#!/usr/bin/env bash

matches_implementation_path() {
  local path="$1"
  case "$path" in
    src/*|web/*|web-legacy/*|tests/*|contracts/*|build/*|deploy/*|database/*) return 0 ;;
    .github/workflows/implementation.yml) return 0 ;;
    .config/dotnet-tools.json|global.json|Directory.Build.props|Directory.Build.targets|Directory.Packages.props|FlexAgent.slnx|nuget.config) return 0 ;;
    package.json|pnpm-lock.yaml|pnpm-workspace.yaml|.nvmrc) return 0 ;;
    gitleaks.toml|.gitleaksignore) return 0 ;;
    *) return 1 ;;
  esac
}
