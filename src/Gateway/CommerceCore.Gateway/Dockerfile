# Build from the repo root:  docker build -f src/Gateway/CommerceCore.Gateway/Dockerfile .

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["src/Gateway/CommerceCore.Gateway/CommerceCore.Gateway.csproj", "src/Gateway/CommerceCore.Gateway/"]
COPY ["src/BuildingBlocks/CommerceCore.BuildingBlocks/CommerceCore.BuildingBlocks.csproj", "src/BuildingBlocks/CommerceCore.BuildingBlocks/"]

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "src/Gateway/CommerceCore.Gateway/CommerceCore.Gateway.csproj" --disable-parallel

COPY src/Gateway/CommerceCore.Gateway/ src/Gateway/CommerceCore.Gateway/
COPY src/BuildingBlocks/CommerceCore.BuildingBlocks/ src/BuildingBlocks/CommerceCore.BuildingBlocks/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish "src/Gateway/CommerceCore.Gateway/CommerceCore.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

USER app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CommerceCore.Gateway.dll"]
