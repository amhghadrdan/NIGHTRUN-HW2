#!/usr/bin/env bash
set -euo pipefail
task_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
task_cli="$task_root/LocalTools/REA-CLI/node_modules/rea-agents/scripts/rea.mjs"
if [[ ! -f "$task_cli" ]]; then
  echo "Local REA CLI missing. Install with: npm install --prefix LocalTools/REA-CLI rea-agents@6.7.0" >&2
  exit 1
fi
exec node "$task_cli" "$@"
