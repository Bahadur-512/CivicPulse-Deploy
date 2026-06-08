# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY CivicPulse.sln .
COPY CivicPulse.Core/CivicPulse.Core.csproj CivicPulse.Core/
COPY CivicPulse.Infrastructure/CivicPulse.Infrastructure.csproj CivicPulse.Infrastructure/
COPY CivicPulse.Web/CivicPulse.Web.csproj CivicPulse.Web/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build and publish
RUN dotnet publish CivicPulse.Web/CivicPulse.Web.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install SQLite runtime library
RUN apt-get update && apt-get install -y libsqlite3-0 && rm -rf /var/lib/apt/lists/*

# Create directory for SQLite database and uploads
RUN mkdir -p /app/data /app/wwwroot/uploads

COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:10000

# Expose port (Render assigns PORT dynamically)
EXPOSE 10000

# Start the application
ENTRYPOINT ["sh", "-c", "dotnet CivicPulse.Web.dll --urls http://+:${PORT:-10000}"]
