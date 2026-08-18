# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0.100-noble@sha256:c7445f141c04f1a6b454181bd098dcfa606c61ba0bd213d0a702489e5bd4cd71 AS build
WORKDIR /src

COPY FlexAgent.slnx global.json Directory.Build.props Directory.Build.targets Directory.Packages.props nuget.config ./
COPY src/BuildingBlocks/FlexAgent.CanonicalJson/ src/BuildingBlocks/FlexAgent.CanonicalJson/
COPY src/Infrastructure/FlexAgent.Postgres/ src/Infrastructure/FlexAgent.Postgres/
COPY src/Modules/IdentityAccess/FlexAgent.IdentityAccess/ src/Modules/IdentityAccess/FlexAgent.IdentityAccess/
COPY src/Modules/Sessions/FlexAgent.Sessions/ src/Modules/Sessions/FlexAgent.Sessions/
COPY src/Modules/Sessions/FlexAgent.Sessions.Infrastructure/ src/Modules/Sessions/FlexAgent.Sessions.Infrastructure/
COPY src/Hosts/FlexAgent.Worker/ src/Hosts/FlexAgent.Worker/
COPY contracts/schemas/v1/common/primitives.v1.schema.json contracts/schemas/v1/common/primitives.v1.schema.json
COPY contracts/schemas/v2/session/agent-decision.v2.schema.json contracts/schemas/v2/session/agent-decision.v2.schema.json
COPY build/toolchain.json build/toolchain.json

RUN dotnet restore src/Hosts/FlexAgent.Worker/FlexAgent.Worker.csproj --locked-mode
RUN dotnet publish src/Hosts/FlexAgent.Worker/FlexAgent.Worker.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0.8-noble@sha256:8c0b6857eab7b2aa57884c839bf4678414606bd7d17370f18a842ac5cf414711 AS final
WORKDIR /app

RUN groupadd --gid 10001 appgroup \
    && useradd --uid 10001 --gid appgroup --create-home --home-dir /home/appuser --shell /usr/sbin/nologin appuser \
    && chown -R appuser:appgroup /app

COPY --from=build /app/publish .
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FlexAgent.Worker.dll"]
