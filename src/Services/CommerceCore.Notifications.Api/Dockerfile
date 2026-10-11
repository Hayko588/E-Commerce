# Build from the repo root:  docker build -f src/Services/CommerceCore.Notifications.Api/Dockerfile .

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]

COPY ["src/BuildingBlocks/CommerceCore.Contracts/CommerceCore.Contracts.csproj", "src/BuildingBlocks/CommerceCore.Contracts/"]
COPY ["src/Services/CommerceCore.Notifications.Api/CommerceCore.Notifications.Api.csproj", "src/Services/CommerceCore.Notifications.Api/"]
COPY ["src/Services/CommerceCore.Notifications.Application/CommerceCore.Notifications.Application.csproj", "src/Services/CommerceCore.Notifications.Application/"]
COPY ["src/Services/CommerceCore.Notifications.Domain/CommerceCore.Notifications.Domain.csproj", "src/Services/CommerceCore.Notifications.Domain/"]
COPY ["src/Services/CommerceCore.Notifications.Infrastructure/CommerceCore.Notifications.Infrastructure.csproj", "src/Services/CommerceCore.Notifications.Infrastructure/"]

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "src/Services/CommerceCore.Notifications.Api/CommerceCore.Notifications.Api.csproj" --disable-parallel

COPY src/BuildingBlocks/CommerceCore.Contracts/ src/BuildingBlocks/CommerceCore.Contracts/
COPY src/Services/CommerceCore.Notifications.Api/ src/Services/CommerceCore.Notifications.Api/
COPY src/Services/CommerceCore.Notifications.Application/ src/Services/CommerceCore.Notifications.Application/
COPY src/Services/CommerceCore.Notifications.Domain/ src/Services/CommerceCore.Notifications.Domain/
COPY src/Services/CommerceCore.Notifications.Infrastructure/ src/Services/CommerceCore.Notifications.Infrastructure/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish "src/Services/CommerceCore.Notifications.Api/CommerceCore.Notifications.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

USER app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CommerceCore.Notifications.Api.dll"]