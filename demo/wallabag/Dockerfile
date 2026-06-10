# demo/wallabag/Dockerfile — one-shot provisioner for the demo Wallabag.
# Tiny alpine + curl + jq + bash; verifies the OAuth grant and writes the env file.
FROM alpine:3.20

RUN apk add --no-cache bash curl jq ca-certificates

COPY bootstrap.sh /bootstrap.sh
RUN chmod +x /bootstrap.sh

ENTRYPOINT ["/bootstrap.sh"]
