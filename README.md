# Blazor-OpenBanking

Blazor web app to pull Open Banking data

## Running the app

```
$ dotnet watch
```


## User Secrets

The following secrets need to be set in a place readable by [configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/).

```
TrueLayer:ApiUrl // https://api.truelayer-sandbox.com
TrueLayer:AuthUrl // https://auth.truelayer-sandbox.com
TrueLayer:ClientId
TrueLayer:ClientSecret
TrueLayer:EnableMock
TrueLayer:RedirectURL
```

e.g.
```
$ dotnet user-secrets set "TrueLayer:ApiUrl" "123"
```

On linux this is stored in `~/.microsoft/usersecrets/{guid}/secrets.json`.

Env var `ASPNETCORE_ENVIRONMENT=Development` will need to be set to read this.
