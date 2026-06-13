# demo/paperless/Dockerfile — one-shot provisioner for the demo paperless-ngx.
# Tiny alpine + curl + jq + bash; obtains an API token and exits.
FROM alpine:3.20

RUN apk add --no-cache bash curl jq ca-certificates

COPY bootstrap.sh /bootstrap.sh
RUN chmod +x /bootstrap.sh

ENTRYPOINT ["/bootstrap.sh"]
