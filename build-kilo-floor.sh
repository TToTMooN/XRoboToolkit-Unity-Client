#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
    echo "Usage: $0 /path/to/2022.3.16f1/Editor/Unity [output.apk]" >&2
    exit 2
fi
kilo_editor=$1
kilo_project=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
kilo_output=${2:-"$kilo_project/Builds/KILO-XR-Floor.apk"}
if [[ ! -x "$kilo_editor" ]]; then
    echo "Unity editor is not executable: $kilo_editor" >&2
    exit 2
fi
if [[ "$kilo_output" != /* ]]; then
    kilo_output="$PWD/$kilo_output"
fi
mkdir -p -- "$(dirname -- "$kilo_output")"
exec "$kilo_editor" -batchmode -nographics -quit -buildTarget Android \
    -projectPath "$kilo_project" -executeMethod KiloFloorBuild.Build \
    -kiloOutput "$kilo_output" -logFile "$kilo_output.build.log"
