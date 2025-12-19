# Use the official .NET 9.0 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project files and restore dependencies
COPY ["HealthSync.WebApi/HealthSync.WebApi.csproj", "HealthSync.WebApi/"]
COPY ["HealthSync.Application/HealthSync.Application.csproj", "HealthSync.Application/"]
COPY ["HealthSync.Domain/HealthSync.Domain.csproj", "HealthSync.Domain/"]
COPY ["HealthSync.Infrastructure/HealthSync.Infrastructure.csproj", "HealthSync.Infrastructure/"]
RUN dotnet restore "HealthSync.WebApi/HealthSync.WebApi.csproj"

# Copy the entire source code and build the application
COPY . .
WORKDIR "/src/HealthSync.WebApi"
RUN dotnet build "HealthSync.WebApi.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "HealthSync.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 9.0 ASP.NET Core runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends wget && rm -rf /var/lib/apt/lists/*

# Expose port 80
EXPOSE 80

# Switch to non-root user for security
USER app

# Set the entry point
ENTRYPOINT ["dotnet", "HealthSync.WebApi.dll"]