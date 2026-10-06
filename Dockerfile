# syntax=docker/dockerfile:1
# One build stage, three runtime targets used by compose.yaml: web, migrate, functions.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ST10382638_CLDV_POE/ST10382638_CLDV_POE.csproj ST10382638_CLDV_POE/
COPY FunctionApp/FunctionApp.csproj FunctionApp/
RUN dotnet restore FunctionApp/FunctionApp.csproj
COPY . .
RUN dotnet publish ST10382638_CLDV_POE -c Release -o /out/web --no-restore \
 && dotnet publish FunctionApp -c Release -o /out/functions --no-restore
WORKDIR /src/ST10382638_CLDV_POE
RUN dotnet tool restore \
 && dotnet ef migrations bundle --configuration Release -o /out/efbundle

# Web app (MVC)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS web
WORKDIR /app
COPY --from=build /out/web .
ENTRYPOINT ["dotnet", "ST10382638_CLDV_POE.dll"]

# Applies EF Core migrations, then exits
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS migrate
COPY --from=build /out/efbundle /efbundle
ENTRYPOINT ["/efbundle"]

# Azure Functions host (isolated worker)
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS functions
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    AzureWebJobsSecretStorageType=files
# Local-only function key so the web app's "?code=local" URLs are accepted.
COPY <<'EOF' /azure-functions-host/Secrets/host.json
{"masterKey":{"name":"master","value":"local","encrypted":false},"functionKeys":[{"name":"default","value":"local","encrypted":false}],"systemKeys":[]}
EOF
COPY --from=build /out/functions /home/site/wwwroot
