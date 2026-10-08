# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Shared build settings must be present at restore time
COPY ["Directory.Build.props", "./"]

# Copy csproj files first to maximize Docker layer caching
COPY ["OrderService.Api/OrderService.Api.csproj", "OrderService.Api/"]
COPY ["OrderService.Application/OrderService.Application.csproj", "OrderService.Application/"]
COPY ["OrderService.Domain/OrderService.Domain.csproj", "OrderService.Domain/"]
COPY ["OrderService.Infrastructure/OrderService.Infrastructure.csproj", "OrderService.Infrastructure/"]

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "./OrderService.Api/OrderService.Api.csproj" --disable-parallel

# Copy remaining source code
COPY . .
WORKDIR "/src/OrderService.Api"

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "OrderService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

USER app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderService.Api.dll"]