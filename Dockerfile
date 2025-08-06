# Use the official .NET 8.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj files and restore dependencies
COPY ["odd-scout/OddScout.API.csproj", "odd-scout/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]

RUN dotnet restore "odd-scout/OddScout.API.csproj"

# Copy the rest of the source code
COPY . .


# Publish the application directly (skip separate build step)
WORKDIR "/app/odd-scout"
RUN dotnet publish "OddScout.API.csproj" -c Release -o /app/publish --no-restore -p:TreatWarningsAsErrors=false -p:UseAppHost=false

# Use Playwright base image with .NET runtime
FROM mcr.microsoft.com/playwright/dotnet:v1.40.0-jammy AS final

# Install .NET 8 runtime on the Playwright image
RUN apt-get update && apt-get install -y --no-install-recommends \
    wget \
    ca-certificates \
    && wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y aspnetcore-runtime-8.0 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*


WORKDIR /app
COPY --from=build /app/publish .

# Expose the port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://*:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "OddScout.API.dll"]