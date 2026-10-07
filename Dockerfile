# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files first to maximize Docker layer caching
COPY ["OrderService.Api/OrderService.Api.csproj", "OrderService.Api/"]
COPY ["OrderService.Application/OrderService.Application.csproj", "OrderService.Application/"]
COPY ["OrderService.Domain/OrderService.Domain.csproj", "OrderService.Domain/"]
COPY ["OrderService.Infrastructure/OrderService.Infrastructure.csproj", "OrderService.Infrastructure/"]

RUN dotnet restore "./OrderService.Api/OrderService.Api.csproj"

# Copy remaining source code
COPY . .

# Change to the correct relative working directory
WORKDIR "/src/OrderService.Api"

# Publish the API
RUN dotnet publish "OrderService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Security Best Practice: Use built-in non-root user 'app' available in .NET 8/9 images
USER app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderService.Api.dll"]