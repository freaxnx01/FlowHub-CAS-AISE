FROM alpine:3.20

RUN apk add --no-cache postgresql-client bash tzdata ca-certificates curl jq

COPY reset.sh /reset.sh
COPY seed.sql /seed.sql
RUN chmod +x /reset.sh

ENV RESET_INTERVAL_SECONDS=900

# Simple sleep-loop rather than crond — one less moving part, identical effect
# at this cadence. Each iteration logs the reset to stdout (collected by
# Docker logs) so the operator can verify it's running.
CMD ["/bin/bash", "-c", "while true; do sleep \"$RESET_INTERVAL_SECONDS\" && /reset.sh; done"]
