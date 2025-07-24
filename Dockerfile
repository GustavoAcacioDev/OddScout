FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["odd-scout/OddScout.API.csproj", "odd-scout/"]
COPY ["Application/OddScout.Application.csproj", "Application/"]
COPY ["Domain/OddScout.Domain.csproj", "Domain/"]
COPY ["Infrastructure/OddScout.Infrastructure.csproj", "Infrastructure/"]

# Restore dependencies
RUN dotnet restore "odd-scout/OddScout.API.csproj"

# Copy source code
COPY . .

# Build application
WORKDIR "/src/odd-scout"
RUN dotnet build "OddScout.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "OddScout.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "OddScout.API.dll"]