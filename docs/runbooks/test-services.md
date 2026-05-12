# Test Services Environment

A dedicated LXC container hosts disposable instances of the productivity services FlowHub integrates with (Vikunja, Wallabag, Paperless-ngx, Forgejo) — fully isolated from the production instances.

## Host

- **Proxmox host:** `cirrus-pve` (192.168.1.111)
- **Container:** CT 128 `flowhub-test-services`
- **IP:** 192.168.1.105
- **Stack:** Debian + Docker + Traefik v3.6 (TLS via internal CA)
- **Storage:** `/home/admin/mydocker` (compose), `/home/admin/container-data/flowhub-test-services` (volumes, on NFS)

Start/stop from any host with SSH access:

```bash
ssh cirrus-pve "pct start 128"   # or: pct stop 128
```

## Services & Endpoints

| Service       | Hostname                                                  | API base    | Auth header                  |
| ------------- | --------------------------------------------------------- | ----------- | ---------------------------- |
| Vikunja       | `todo.flowhub-test-services.home.freaxnx01.ch`            | `/api/v1`   | `Authorization: Bearer tk_…` |
| Wallabag      | `read-later.flowhub-test-services.home.freaxnx01.ch`      | `/api`      | OAuth2 (see below)           |
| Paperless-ngx | `dms.flowhub-test-services.home.freaxnx01.ch`             | `/api`      | `Authorization: Token …`     |
| Forgejo       | `git.flowhub-test-services.home.freaxnx01.ch` (SSH :222)  | `/api/v1`   | `Authorization: token …`     |

> **DNS caveat:** the `*.flowhub-test-services.home.freaxnx01.ch` names currently resolve to the homelab Traefik dispatcher (192.168.1.101), but no dispatcher route exists yet — TLS returns `unrecognized_name`. Until that's fixed, clients must target the CT directly:
>
> ```bash
> curl --resolve todo.flowhub-test-services.home.freaxnx01.ch:443:192.168.1.105 \
>      https://todo.flowhub-test-services.home.freaxnx01.ch/api/v1/info
> ```

## Accounts

| Service   | Admin user | Notes                            |
| --------- | ---------- | -------------------------------- |
| Vikunja   | `admin`    | email `admin@freaxnx01.ch`       |
| Wallabag  | `wallabag` | registration disabled            |
| Paperless | `admin`    | Django superuser                 |
| Forgejo   | `gitadmin` | site admin                       |

## Wallabag OAuth2 flow

```bash
curl -X POST https://read-later.flowhub-test-services.home.freaxnx01.ch/oauth/v2/token \
  -d "grant_type=password" \
  -d "client_id=<id>"  -d "client_secret=<secret>" \
  -d "username=wallabag" -d "password=<pw>"
# → { "access_token": "...", "refresh_token": "...", "expires_in": 3600 }
```

Use the returned `access_token` as `Authorization: Bearer <access_token>` against `/api`.

## Secrets — Passbolt

All credentials live in Passbolt under the `flowhub-test-services/` prefix. **Never commit these.**

| Resource name                                  | Holds                                         |
| ---------------------------------------------- | --------------------------------------------- |
| `flowhub-test-services/vikunja API token`      | Bearer token (full perms, expires 2027-05-12) |
| `flowhub-test-services/vikunja admin`          | Web login for `admin`                         |
| `flowhub-test-services/wallabag OAuth2 client` | client_id (username) / client_secret (pw)     |
| `flowhub-test-services/wallabag user`          | `wallabag` web + password-grant credentials   |
| `flowhub-test-services/forgejo API token`      | API token, scope=all                          |
| `flowhub-test-services/paperless API token`    | DRF Token for `admin`                         |
| `flowhub-test-services/paperless admin`        | Web login for `admin`                         |

Fetch via Passbolt CLI:

```bash
passbolt list resource --filter 'matches(Name, "flowhub-test-services")'
passbolt get resource <id> -j   # JSON incl. decrypted password
```

For local development, prefer `passbolt exec` to inject secrets as env vars instead of writing them to disk.
