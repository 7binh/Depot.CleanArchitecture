#!/usr/bin/env sh
# Single quality gate for humans and every AI agent. Exit != 0 means the code is not clean.
set -eu
cd "$(dirname "$0")/.."

echo "==> Backend: build with Roslyn + SonarAnalyzer (warnings are errors)"
dotnet build Depot.slnx --nologo -v q

if [ -f tests/.gitkeep ] && [ "$(find tests -name '*.csproj' | wc -l)" -eq 0 ]; then
  echo "==> Backend tests: none yet, skipped"
else
  dotnet test Depot.slnx --nologo -v q --no-build
fi

echo "==> Frontend: ESLint (angular-eslint + sonarjs) + Prettier"
cd web
npm run lint --silent
npm run format:check --silent
