# Builder: runs on the platform of the build machine and compiles for the target one (linux/amd64, as the free EC2 of
# AWS, or linux/arm64, as Graviton), so a multi-arch image needs no emulation to compile
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
ARG TARGETARCH
ARG DOTNET_CONFIGURATION=Release
WORKDIR /App

COPY ./ ./
RUN dotnet restore ./src/PromoFilterBot.Host/PromoFilterBot.Host.csproj -a $TARGETARCH

RUN dotnet publish ./src/PromoFilterBot.Host/PromoFilterBot.Host.csproj -c "${DOTNET_CONFIGURATION}" -a $TARGETARCH --no-restore -o out

# Final image: only the runtime, no ASP.NET (the bot uses long polling and opens no port)
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base

WORKDIR /App

ENV DOTNET_ENVIRONMENT=Production

COPY --from=builder /App/out .

# The session of the reader account and the logs live in volumes, owned by the non-root user of the image
RUN mkdir -p /App/session /App/logs && chown -R $APP_UID /App/session /App/logs
USER $APP_UID

VOLUME ["/App/session", "/App/logs"]

ENTRYPOINT ["dotnet", "PromoFilterBot.Host.dll"]
