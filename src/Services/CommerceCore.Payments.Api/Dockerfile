# Build from the repo root:  docker build -f src/Services/CommerceCore.Payments.Api/Dockerfile .

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "./"]

COPY ["src/BuildingBlocks/CommerceCore.Contracts/CommerceCore.Contracts.csproj", "src/BuildingBlocks/CommerceCore.Contracts/"]
COPY ["src/Services/CommerceCore.Payments.Api/CommerceCore.Payments.Api.csproj", "src/Services/CommerceCore.Payments.Api/"]
COPY ["src/Services/CommerceCore.Payments.Application/CommerceCore.Payments.Application.csproj", "src/Services/CommerceCore.Payments.Application/"]
COPY ["src/Services/CommerceCore.Payments.Domain/CommerceCore.Payments.Domain.csproj", "src/Services/CommerceCore.Payments.Domain/"]
COPY ["src/Services/CommerceCore.Payments.Infrastructure/CommerceCore.Payments.Infrastructure.csproj", "src/Services/CommerceCore.Payments.Infrastructure/"]
COPY ["src/BuildingBlocks/CommerceCore.BuildingBlocks/CommerceCore.BuildingBlocks.csproj", "src/BuildingBlocks/CommerceCore.BuildingBlocks/"]
COPY ["dotnet-tools.json", ".config/"]

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "src/Services/CommerceCore.Payments.Api/CommerceCore.Payments.Api.csproj" --disable-parallel

COPY src/BuildingBlocks/CommerceCore.Contracts/ src/BuildingBlocks/CommerceCore.Contracts/
COPY src/Services/CommerceCore.Payments.Api/ src/Services/CommerceCore.Payments.Api/
COPY src/Services/CommerceCore.Payments.Application/ src/Services/CommerceCore.Payments.Application/
COPY src/Services/CommerceCore.Payments.Domain/ src/Services/CommerceCore.Payments.Domain/
COPY src/Services/CommerceCore.Payments.Infrastructure/ src/Services/CommerceCore.Payments.Infrastructure/
COPY src/BuildingBlocks/CommerceCore.BuildingBlocks/ src/BuildingBlocks/CommerceCore.BuildingBlocks/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish "src/Services/CommerceCore.Payments.Api/CommerceCore.Payments.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 1b: EF Core migration bundle. It runs as a Kubernetes Job, never inside the API container.
FROM build AS migrations-build
# The app is constructed (not run) to discover the DbContext, so it needs placeholder settings.
ENV ConnectionStrings__DefaultConnection="Server=bundle;Database=bundle;User Id=bundle;Password=bundle;TrustServerCertificate=True" \
    Services__Catalog__BaseUrl="http://localhost"
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet tool restore && \
    dotnet ef migrations bundle \
      --project src/Services/CommerceCore.Payments.Infrastructure \
      --startup-project src/Services/CommerceCore.Payments.Api \
      --self-contained -r linux-x64 --force -o /out/efbundle

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrator
WORKDIR /app
COPY --from=migrations-build /out/efbundle ./efbundle
USER app
ENTRYPOINT ["./efbundle"]

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

USER app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CommerceCore.Payments.Api.dll"]
