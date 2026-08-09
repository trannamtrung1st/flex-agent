# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0.100-noble AS build
WORKDIR /src

COPY FlexAgent.slnx global.json Directory.Build.props Directory.Build.targets Directory.Packages.props nuget.config ./
COPY src/Hosts/FlexAgent.Api/ src/Hosts/FlexAgent.Api/
COPY build/toolchain.json build/toolchain.json

RUN dotnet restore src/Hosts/FlexAgent.Api/FlexAgent.Api.csproj --locked-mode
RUN dotnet publish src/Hosts/FlexAgent.Api/FlexAgent.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0.0-noble AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --gid 10001 appgroup \
    && useradd --uid 10001 --gid appgroup --create-home --home-dir /home/appuser --shell /usr/sbin/nologin appuser \
    && chown -R appuser:appgroup /app

COPY --from=build /app/publish .
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/health/live >/dev/null || exit 1

ENTRYPOINT ["dotnet", "FlexAgent.Api.dll"]
