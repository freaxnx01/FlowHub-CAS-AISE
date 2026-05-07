# Authentik OIDC Setup

This runbook documents how to register FlowHub as an OAuth2/OIDC client in Authentik and configure the environment variables.

## Authentik Configuration

1. Log into Authentik admin (`https://authentik.home.freaxnx01.ch/if/admin/`).
2. Navigate to **Applications → Providers → Create → OAuth2/OpenID Provider**.
3. Configure:
   - **Name:** `flowhub`
   - **Client type:** Confidential
   - **Client ID:** `flowhub` (copy this value)
   - **Client Secret:** (generate and copy)
   - **Redirect URIs:** `https://<flowhub-host>/signin-oidc`
   - **Post-logout redirect URIs:** `https://<flowhub-host>/signout-callback-oidc`
   - **Scopes:** `openid profile email`
4. Navigate to **Applications → Applications → Create**.
   - **Name:** `FlowHub`
   - **Slug:** `flowhub`
   - **Provider:** select the provider created above
5. Note the **OpenID Configuration URL**: `https://authentik.home.freaxnx01.ch/application/o/flowhub/.well-known/openid-configuration`

## FlowHub Environment Variables

Set these in your `.env` file or secrets manager (never commit):

```env
Auth__OIDC__Authority=https://authentik.home.freaxnx01.ch/application/o/flowhub/
Auth__OIDC__ClientId=flowhub
Auth__OIDC__ClientSecret=<secret from Authentik>
```

## Demo Mode (no OIDC)

Omit all `Auth__OIDC__*` variables. FlowHub will use `DemoAuthHandler` and auto-sign in as "Demo Operator". This is the default for `docker-compose.override.yml`.

## Verification

After setting env vars and restarting:
1. Open FlowHub in browser.
2. Should redirect to Authentik login page.
3. After login, redirected back to FlowHub with the authenticated user's name in the top bar.
