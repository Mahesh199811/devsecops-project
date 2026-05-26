#!/usr/bin/env bash
set -euo pipefail

IMAGE_NAME="${IMAGE_NAME:-devsecops-project:latest}"

trivy image --exit-code 1 --severity HIGH,CRITICAL "$IMAGE_NAME"
