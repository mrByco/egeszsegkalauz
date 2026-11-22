# syntax=docker/dockerfile:1

FROM node:24-alpine AS client
WORKDIR /client
COPY client/package.json client/package-lock.json ./
RUN npm ci
COPY client/ .
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY egeszsegkalauz.app/Directory.Build.props egeszsegkalauz.app/Directory.Packages.props ./
COPY egeszsegkalauz.app/egeszsegkalauz.api/egeszsegkalauz.api.csproj egeszsegkalauz.api/
COPY egeszsegkalauz.app/egeszsegkalauz.services/egeszsegkalauz.services.csproj egeszsegkalauz.services/
COPY egeszsegkalauz.app/egeszsegkalauz.data/egeszsegkalauz.data.csproj egeszsegkalauz.data/
RUN dotnet restore egeszsegkalauz.api/egeszsegkalauz.api.csproj
COPY egeszsegkalauz.app/ .
RUN dotnet publish egeszsegkalauz.api/egeszsegkalauz.api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
COPY --from=api /app .
COPY --from=client /client/dist/client/browser wwwroot/
ENTRYPOINT ["dotnet", "egeszsegkalauz.api.dll"]
