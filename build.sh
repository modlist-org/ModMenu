#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG="${1:-Release_ML}"
if [[ $# -gt 0 ]]; then shift; fi

dotnet build "$ROOT/ModMenu/ModMenu.csproj" -c "$CONFIG" "$@"
