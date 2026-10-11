# Build from the repo root:  docker build -f src/Services/CommerceCore.Payments.Api/Dockerfile .

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]

COPY ["src/BuildingBlocks/CommerceCore.Contracts/CommerceCore.Contracts.csproj", "src/BuildingBlocks/CommerceCore.Contracts/"]
COPY ["src/Services/CommerceCore.Payments.Api/CommerceCore.Payments.Api.csproj", "src/Services/CommerceCore.Payments.Api/"]
COPY ["src/Services/CommerceCore.Payments.Application/CommerceCore.Payments.Application.csproj", "src/Services/CommerceCore.Payments.Application/"]
COPY ["src/Services/CommerceCore.Payments.Domain/CommerceCore.Payments.Domain.csproj", "src/Services/CommerceCore.Payments.Domain/"]
COPY ["src/Services/CommerceCore.Payments.Infrastructure/CommerceCore.Payments.Infrastructure.csproj", "src/Services/CommerceCore.Payments.Infrastructure/"]

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "src/Services/CommerceCore.Payments.Api/CommerceCore.Payments.Api.csproj" --disable-parallel

COPY src/BuildingBlocks/CommerceCore.Contracts/ src/BuildingBlocks/CommerceCore.Contracts/
COPY src/Services/CommerceCore.Payments.Api/ src/Services/CommerceCore.Payments.Api/
COPY src/Services/CommerceCore.Payments.Application/ src/Services/CommerceCore.Payments.Application/
COPY src/Services/CommerceCore.Payments.Domain/ src/Services/CommerceCore.Payments.Domain/
COPY src/Services/CommerceCore.Payments.Infrastructure/ src/Services/CommerceCore.Payments.Infrastructure/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish "src/Services/CommerceCore.Payments.Api/CommerceCore.Payments.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

USER app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CommerceCore.Payments.Api.dll"]
