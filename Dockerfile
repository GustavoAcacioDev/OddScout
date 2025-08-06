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

# Install Playwright CLI and browsers in build stage
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN playwright install chromium --with-deps

# Publish the application directly (skip separate build step)
WORKDIR "/app/odd-scout"
RUN dotnet publish "OddScout.API.csproj" -c Release -o /app/publish --no-restore -p:TreatWarningsAsErrors=false -p:UseAppHost=false

# Use a custom runtime image with Node.js for Playwright
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Install Node.js and npm for Playwright
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    gnupg \
    ca-certificates \
    && curl -fsSL https://deb.nodesource.com/setup_18.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# Install Playwright dependencies
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
    libgconf-2-4 \
    libasound2 \
    libpangocairo-1.0-0 \
    libatk1.0-0 \
    libcairo-gobject2 \
    libgdk-pixbuf2.0-0 \
    libxss1 \
    && rm -rf /var/lib/apt/lists/*

# Copy Playwright browsers from build stage
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright

WORKDIR /app
COPY --from=build /app/publish .

# Expose the port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://*:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "OddScout.API.dll"]