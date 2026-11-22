# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["egeszsegkalauz.api/egeszsegkalauz.api.csproj", "egeszsegkalauz.api/"]
COPY ["egeszsegkalauz.services/egeszsegkalauz.services.csproj", "egeszsegkalauz.services/"]
COPY ["egeszsegkalauz.data/egeszsegkalauz.data.csproj", "egeszsegkalauz.data/"]
RUN dotnet restore "egeszsegkalauz.api/egeszsegkalauz.api.csproj"

COPY . .
RUN dotnet publish "egeszsegkalauz.api/egeszsegkalauz.api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "egeszsegkalauz.api.dll"]
