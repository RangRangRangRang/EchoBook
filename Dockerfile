# syntax=docker/dockerfile:1

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY EchoBook/EchoBook.csproj EchoBook/
RUN dotnet restore EchoBook/EchoBook.csproj

COPY EchoBook/ EchoBook/
WORKDIR /src/EchoBook
RUN dotnet publish EchoBook.csproj -c Release -o /app/publish --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Uploaded epubs, extracted covers, and generated TTS audio are runtime data, not part of the
# image - they're written under these two folders, which should be mounted as a persistent
# volume in production (see docker-compose.yml / README) so they survive redeploys.
RUN mkdir -p /app/Uploads /app/AudioCache

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
# Most free hosts (Render, Railway, ...) set PORT themselves at runtime; 8080 is the default
# when running the container standalone (e.g. via docker-compose).
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EchoBook.dll"]