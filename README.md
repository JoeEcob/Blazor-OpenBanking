# Blazor-OpenBanking

Blazor web app to pull Open Banking data.

## Running the app

```sh
$ dotnet watch
```

Transactions will be fetched every 6 hours (set in `Loader.cs`).

## User Secrets

The following secrets need to be set in a place readable by [configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/).

```
TrueLayer:ApiUrl // https://api.truelayer{-sandbox}.com
TrueLayer:AuthUrl // https://auth.truelayer{-sandbox}.com
TrueLayer:ClientId
TrueLayer:ClientSecret
TrueLayer:EnableMock
TrueLayer:RedirectURL
```

e.g.
```sh
$ dotnet user-secrets set "TrueLayer:ApiUrl" "123"
```

On linux this is stored in `~/.microsoft/usersecrets/{guid}/secrets.json`.

Env var `ASPNETCORE_ENVIRONMENT=Development` will need to be set to read this.

## Hosting

Create 3 files in a folder: `.env`, `compose.yml`, `caddyfile`.

compose.yml
```yml
services:
  web:
    image: ghcr.io/joeecob/blazor-openbanking:latest
    container_name: blazor-ob
    user: ${USER_ID}:${GROUP_ID}
    volumes:
      - ./data:/app/AppData
    restart: unless-stopped
    environment:
      - TrueLayer:ApiUrl=${TL_API_URL}
      - TrueLayer:AuthUrl=${TL_AUTH_URL}
      - TrueLayer:ClientId=${TL_CLIENT_ID}
      - TrueLayer:ClientSecret=${TL_CLIENT_SECRET}
      - TrueLayer:EnableMock=${TL_ENABLE_MOCK}
      - TrueLayer:RedirectURL=${TL_REDIRECT_URL}

  caddy:
    image: caddy:alpine
    container_name: blazor-caddy
    ports:
      - "5007:5007"
    volumes:
      - ./caddyfile:/etc/caddy/Caddyfile
      - caddy_data:/data
      - caddy_certs:/config
    restart: unless-stopped

volumes:
  caddy_data:
  caddy_certs:
```

caddyfile
```json
:5007 {
    tls internal
    reverse_proxy web:8080
}
```
