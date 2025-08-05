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

# Use the official .NET 8.0 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Install dependencies for Playwright (web scraping)
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    ca-certificates \
    procps \
    libxss1 \
    && rm -rf /var/lib/apt/lists/*

# Install Playwright browsers and dependencies
RUN apt-get update && apt-get install -y \
    libnss3 \
    libatk-bridge2.0-0 \
    libdrm2 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    libgbm1 \
    libxkbcommon0 \
    libgtk-3-0 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Install Playwright
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="$PATH:/root/.dotnet/tools"

# Install Playwright browsers
RUN playwright install chromium
RUN playwright install-deps chromium

# Expose the port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://*:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "OddScout.API.dll"]