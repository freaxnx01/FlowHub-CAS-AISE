#!/usr/bin/env bash
# tools/wallabag-token.sh
#
# Fetches a fresh Wallabag OAuth2 access_token via the password-grant flow
# against the flowhub-test-services CT 128 instance, using credentials
# stored in Passbolt under the `flowhub-test-services/` prefix.
#
# Why a helper script instead of `passbolt://` resolution in .env: Wallabag
# tokens expire in 3600 s and require four inputs (client id/secret +
# user/password) plus a network call to /oauth/v2/token. There is no
# long-lived bearer to commit. The script does the exchange on demand.
#
# Outputs:
#   default     — prints the access_token on stdout (single line)
#   --export    — prints `export Skills__Wallabag__BaseUrl=...` plus the four
#                 OAuth credential keys (ClientId/ClientSecret/Username/Password)
#                 the skill binds to; suitable for `eval $(tools/wallabag-token.sh --export)`.
#                 (The skill mints + refreshes its own access_token from these.)
#
# Exits non-zero on any failure (Passbolt unauthenticated, network down,
# Wallabag returns an error, missing JSON fields).

set -euo pipefail

# Passbolt resource UUIDs — see docs/runbooks/test-services.md § Secrets.
OAUTH_CLIENT_RESOURCE="9a5ce2da-8680-4e97-a0c7-4ed5a647be2e"
USER_RESOURCE="ca429f0f-878b-49d1-bebf-adb7a1b33d55"

WALLABAG_URL="${Skills__Wallabag__BaseUrl:-https://read-later.flowhub-test-services.home.freaxnx01.ch}"

mode="${1:-}"
case "$mode" in
  ""|--export|--token) ;;
  *)
    echo "usage: $0 [--export | --token]" >&2
    exit 64
    ;;
esac

require() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "error: required command '$1' not found in PATH" >&2
    exit 127
  fi
}
require passbolt
require curl
require python3

field() {
  # field <resource_id> <key>
  passbolt get resource --id "$1" -j 2>/dev/null \
    | python3 -c "import json,sys; d=json.load(sys.stdin); print(d['$2'])"
}

CLIENT_ID="$(field "$OAUTH_CLIENT_RESOURCE" username)"
CLIENT_SECRET="$(field "$OAUTH_CLIENT_RESOURCE" password)"
USERNAME="$(field "$USER_RESOURCE" username)"
USERPW="$(field "$USER_RESOURCE" password)"

if [ -z "$CLIENT_ID" ] || [ -z "$CLIENT_SECRET" ] || [ -z "$USERNAME" ] || [ -z "$USERPW" ]; then
  echo "error: empty credential field from Passbolt — check resource UUIDs" >&2
  exit 1
fi

RESPONSE="$(curl -fsS -X POST "$WALLABAG_URL/oauth/v2/token" \
  --data-urlencode "grant_type=password" \
  --data-urlencode "client_id=$CLIENT_ID" \
  --data-urlencode "client_secret=$CLIENT_SECRET" \
  --data-urlencode "username=$USERNAME" \
  --data-urlencode "password=$USERPW")"

TOKEN="$(echo "$RESPONSE" | python3 -c 'import json,sys; d=json.load(sys.stdin); print(d.get("access_token",""))')"

if [ -z "$TOKEN" ]; then
  echo "error: Wallabag did not return an access_token. Response:" >&2
  echo "$RESPONSE" >&2
  exit 1
fi

if [ "$mode" = "--export" ]; then
  printf 'export Skills__Wallabag__BaseUrl=%q\n' "$WALLABAG_URL"
  printf 'export Skills__Wallabag__ClientId=%q\n' "$CLIENT_ID"
  printf 'export Skills__Wallabag__ClientSecret=%q\n' "$CLIENT_SECRET"
  printf 'export Skills__Wallabag__Username=%q\n' "$USERNAME"
  printf 'export Skills__Wallabag__Password=%q\n' "$USERPW"
else
  echo "$TOKEN"
fi
