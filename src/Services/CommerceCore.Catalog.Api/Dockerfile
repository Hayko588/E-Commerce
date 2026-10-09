# Build from the repo root:  docker build -f src/Services/CommerceCore.Catalog.Api/Dockerfile .

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]

# Copy csproj files first to maximize Docker layer caching
COPY ["src/Services/CommerceCore.Catalog.Api/CommerceCore.Catalog.Api.csproj", "src/Services/CommerceCore.Catalog.Api/"]
COPY ["src/Services/CommerceCore.Catalog.Application/CommerceCore.Catalog.Application.csproj", "src/Services/CommerceCore.Catalog.Application/"]
COPY ["src/Services/CommerceCore.Catalog.Domain/CommerceCore.Catalog.Domain.csproj", "src/Services/CommerceCore.Catalog.Domain/"]
COPY ["src/Services/CommerceCore.Catalog.Infrastructure/CommerceCore.Catalog.Infrastructure.csproj", "src/Services/CommerceCore.Catalog.Infrastructure/"]

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "src/Services/CommerceCore.Catalog.Api/CommerceCore.Catalog.Api.csproj" --disable-parallel

# Only this service's source, so changes to another service don't invalidate this build
COPY src/Services/CommerceCore.Catalog.Api/ src/Services/CommerceCore.Catalog.Api/
COPY src/Services/CommerceCore.Catalog.Application/ src/Services/CommerceCore.Catalog.Application/
COPY src/Services/CommerceCore.Catalog.Domain/ src/Services/CommerceCore.Catalog.Domain/
COPY src/Services/CommerceCore.Catalog.Infrastructure/ src/Services/CommerceCore.Catalog.Infrastructure/

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "src/Services/CommerceCore.Catalog.Api/CommerceCore.Catalog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

USER app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CommerceCore.Catalog.Api.dll"]
