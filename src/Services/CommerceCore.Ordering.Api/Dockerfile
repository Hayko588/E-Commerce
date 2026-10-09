# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]

# Copy csproj files first to maximize Docker layer caching
COPY ["src/Services/CommerceCore.Ordering.Api/CommerceCore.Ordering.Api.csproj", "src/Services/CommerceCore.Ordering.Api/"]
COPY ["src/Services/CommerceCore.Ordering.Application/CommerceCore.Ordering.Application.csproj", "src/Services/CommerceCore.Ordering.Application/"]
COPY ["src/Services/CommerceCore.Ordering.Domain/CommerceCore.Ordering.Domain.csproj", "src/Services/CommerceCore.Ordering.Domain/"]
COPY ["src/Services/CommerceCore.Ordering.Infrastructure/CommerceCore.Ordering.Infrastructure.csproj", "src/Services/CommerceCore.Ordering.Infrastructure/"]

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "src/Services/CommerceCore.Ordering.Api/CommerceCore.Ordering.Api.csproj" --disable-parallel

COPY src/Services/CommerceCore.Ordering.Api/ src/Services/CommerceCore.Ordering.Api/
COPY src/Services/CommerceCore.Ordering.Application/ src/Services/CommerceCore.Ordering.Application/
COPY src/Services/CommerceCore.Ordering.Domain/ src/Services/CommerceCore.Ordering.Domain/
COPY src/Services/CommerceCore.Ordering.Infrastructure/ src/Services/CommerceCore.Ordering.Infrastructure/

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "src/Services/CommerceCore.Ordering.Api/CommerceCore.Ordering.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

USER app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CommerceCore.Ordering.Api.dll"]
