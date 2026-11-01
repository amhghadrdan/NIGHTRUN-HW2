#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ -n "${UNITY_EDITOR_PATH:-}" ]]; then
  editor="$UNITY_EDITOR_PATH"
else
  editor="$(find "$HOME/Unity/Hub/Editor" /opt/Unity/Hub/Editor -path '*/2022.3.62f1/Editor/Unity' -type f 2>/dev/null | head -n 1 || true)"
fi
if [[ -z "$editor" || ! -x "$editor" ]]; then
  printf 'Set UNITY_EDITOR_PATH to the Unity 2022.3.62f1 Editor executable.\n' >&2
  exit 1
fi
# Public players must contain only the redistributable project assets.
# Unity includes every Resources folder, including optional local experiments.
case "${1:-all}" in
  qa|windows|windows-qa|all)
    if [[ -d Assets/LocalAsphalt ]]; then
      excluded_pack="$(mktemp -d /tmp/nightrun-local-pack.XXXXXX)"
      restore_pack() {
        mv "$excluded_pack/LocalAsphalt" Assets/LocalAsphalt
        if [[ -f "$excluded_pack/LocalAsphalt.meta" ]]; then mv "$excluded_pack/LocalAsphalt.meta" Assets/LocalAsphalt.meta; fi
        rmdir "$excluded_pack"
      }
      trap restore_pack EXIT
      mv Assets/LocalAsphalt "$excluded_pack/LocalAsphalt"
      if [[ -f Assets/LocalAsphalt.meta ]]; then mv Assets/LocalAsphalt.meta "$excluded_pack/LocalAsphalt.meta"; fi
    fi
    ;;
esac
mkdir -p Logs Logs/Validation
case "${1:-all}" in
  generate) "$editor" -batchmode -quit -projectPath "$PWD" -executeMethod ProjectBuilder.Generate -logFile "$PWD/Logs/generate.log" ;;
  test) "$editor" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults "$PWD/Logs/Validation/editmode.xml" -logFile "$PWD/Logs/tests.log" ;;
  qa) "$editor" -batchmode -quit -projectPath "$PWD" -executeMethod ProjectBuilder.BuildLinuxQA -logFile "$PWD/Logs/build-qa.log" ;;
  windows) "$editor" -batchmode -quit -projectPath "$PWD" -executeMethod ProjectBuilder.BuildWindows -logFile "$PWD/Logs/build-windows.log" ;;
  windows-qa) "$editor" -batchmode -quit -projectPath "$PWD" -executeMethod ProjectBuilder.BuildWindowsQA -logFile "$PWD/Logs/build-windows-qa.log" ;;
  all) "$editor" -batchmode -quit -projectPath "$PWD" -executeMethod ProjectBuilder.BuildAll -logFile "$PWD/Logs/build.log" ;;
  *) printf 'Usage: %s {generate|test|qa|windows|windows-qa|all}\n' "$0" >&2; exit 2 ;;
esac
