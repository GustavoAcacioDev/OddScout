FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src

# Copy project files
COPY ["odd-scout/*.csproj", "odd-scout/"]
COPY ["Application/*.csproj", "Application/"]
COPY ["Domain/*.csproj", "Domain/"]
COPY ["Infrastructure/*.csproj", "Infrastructure/"]

# Restore with retry logic
RUN dotnet restore "odd-scout/*.csproj" --disable-parallel

# Copy source code
COPY . .

# Build
WORKDIR "/src/odd-scout"
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "OddScout.API.dll"]