FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /source
COPY . .

RUN dotnet restore "BoredGames.sln"
RUN dotnet build "src/BoredGames.Api/BoredGames.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build
FROM build AS publish
RUN dotnet publish "src/BoredGames.Api/BoredGames.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BoredGames.Api.dll"]