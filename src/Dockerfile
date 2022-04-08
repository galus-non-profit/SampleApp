FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY SampleApp.sln SampleApp.sln
COPY SampleApp.WebUI/SampleApp.WebUI.csproj SampleApp.WebUI/SampleApp.WebUI.csproj
RUN dotnet restore

COPY . .

RUN dotnet build -c Release -o /app/build && dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app

RUN apt-get update && apt-get install -y curl=7.74.0-1.3+deb11u1 --no-install-recommends && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN groupadd -g 10000 dotnet && useradd -u 10000 -g dotnet dotnet && chown -R dotnet:dotnet /app
USER dotnet:dotnet

ENV ASPNETCORE_URLS https://*:5443
EXPOSE 5443

HEALTHCHECK --interval=30s --timeout=10s --retries=3 CMD curl --insecure --fail https://localhost:5443/health || exit 1

ENTRYPOINT ["dotnet", "SampleApp.WebUI.dll"]