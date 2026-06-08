# demo/vikunja/Dockerfile — one-shot provisioner for the demo Vikunja instance.
# Tiny alpine + curl + jq + bash; runs demo/vikunja/bootstrap.sh once and exits.
FROM alpine:3.20

RUN apk add --no-cache bash curl jq ca-certificates

COPY bootstrap.sh /bootstrap.sh
RUN chmod +x /bootstrap.sh

ENTRYPOINT ["/bootstrap.sh"]
