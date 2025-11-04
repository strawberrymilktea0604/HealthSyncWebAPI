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
COPY --from=publish /app/publish .

# Expose port 80
EXPOSE 80

# Set the entry point
ENTRYPOINT ["dotnet", "HealthSync.WebApi.dll"]