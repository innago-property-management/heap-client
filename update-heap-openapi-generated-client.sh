#!/usr/bin/env bash

__openapi_source="https://raw.githubusercontent.com/heap/heap-api-reference/refs/heads/main/webhook_spec.yaml"
__destination="src/HeapApiClient/GeneratedHeapClient.cs"

kiota generate \
  --language CSharp \
  --class-name GeneratedHeapClient \
  --openapi "${__openapi_source}" \
  --output "${__destination}"
