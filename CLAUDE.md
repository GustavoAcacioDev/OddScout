# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture Overview

OddScout is a .NET 8.0 Web API implementing Clean Architecture with CQRS pattern for sports betting odds comparison and value betting analysis. The system scrapes odds from Pinnacle (reference) and Betby (opportunities) to identify profitable betting opportunities.

### Key Architectural Patterns
- **Clean Architecture**: Domain → Application → Infrastructure → API layers
- **CQRS with MediatR**: Commands and Queries separated with pipeline behaviors
- **Repository Pattern**: Data access abstraction through Entity Framework Core
- **JWT Authentication**: Token-based auth with refresh token support

### Project Structure
```
├── Domain/              # Core business entities and value objects
├── Application/         # Business logic, Commands/Queries, DTOs
├── Infrastructure/      # Data access, external services, migrations
└── odd-scout/ (API)     # Controllers, middleware, configuration
```

## Development Commands

### Building and Running
```bash
# Build the solution
dotnet build

# Run the API (from root directory)
dotnet run --project odd-scout/OddScout.API.csproj

# Run with specific environment
dotnet run --project odd-scout/OddScout.API.csproj --environment Development
```

### Database Management
```bash
# Add migration
dotnet ef migrations add MigrationName --project Infrastructure --startup-project odd-scout

# Update database
dotnet ef database update --project Infrastructure --startup-project odd-scout

# Drop database (development only)
dotnet ef database drop --project Infrastructure --startup-project odd-scout
```

### Testing and Quality
```bash
# Run tests (if available)
dotnet test

# Format code
dotnet format
```

## Database Configuration

The application supports both SQL Server and PostgreSQL through provider-specific configurations:

- **SQL Server**: Default configurations in `Infrastructure/Data/Configurations/`
- **PostgreSQL**: Specialized configurations in `Infrastructure/Data/Configurations/PostgreSQL/`
- **Provider Selection**: Set `DatabaseProvider` in appsettings.json ("SqlServer" or "PostgreSQL")

Database provider detection happens automatically in `ApplicationDbContext.OnModelCreating()`.

## Key Components

### Core Entities
- **User**: Authentication and user management
- **Event**: Sports events with odds
- **Odd**: Individual betting odds from different sources
- **ValueBet**: Calculated profitable betting opportunities
- **Bet**: User-placed bets with tracking
- **Transaction**: User balance and transaction history

### Scraping Services
- **BetbyScrapingService**: Scrapes odds from Betby using Playwright
- **PinnacleScrapingService**: Scrapes reference odds from Pinnacle
- **ValueBetCalculationService**: Compares odds to identify value bets

### Authentication Flow
- JWT tokens with configurable expiration (default: 60min access, 7 days refresh)
- Password hashing with secure ValueObject pattern
- Email-based password reset functionality

## Configuration

### Required Environment Variables (Production)
```
PGHOST=your-postgres-host
PGDATABASE=your-database-name
PGUSER=your-username
PGPASSWORD=your-password
PGPORT=5432
```

### Important Settings
- JWT configuration in appsettings.json
- Database provider selection
- RapidAPI key for Pinnacle integration
- CORS policy (currently allows all origins for development)

## Development Notes

### Command/Query Pattern
All business operations follow CQRS:
- Commands: `Application/[Entity]/Commands/[Operation]/`
- Queries: `Application/[Entity]/Queries/[Operation]/`
- Each includes Handler and Validator classes

### Validation
FluentValidation is used throughout with pipeline behavior integration. All commands and queries have corresponding validators.

### Exception Handling
Global exception handling through:
- `GlobalExceptionHandler` for unhandled exceptions
- `ApiResponseMiddleware` for consistent response wrapping
- Structured error responses with problem details

### Migration Strategy
Migrations are environment-specific:
- SQL Server migrations in `Infrastructure/Migrations/SqlServer/`
- PostgreSQL migrations in `Infrastructure/Migrations/PostgreSQL/`
- Legacy migrations in root `Infrastructure/Migrations/`

## API Endpoints

### Authentication
- `POST /api/auth/signin` - User login
- `POST /api/auth/signup` - User registration
- `POST /api/auth/refresh` - Token refresh

### Core Features  
- `GET /api/scraping/value-bets` - Get calculated value bets
- `POST /api/scraping/run-betby` - Execute Betby scraping
- `POST /api/scraping/run-pinnacle` - Execute Pinnacle scraping
- `POST /api/scraping/calculate-value-bets` - Calculate value opportunities

### User Management
- `GET /api/users/profile` - Get user profile
- `GET /api/users/transactions` - Transaction history
- `POST /api/users/deposit` - Deposit balance

## Deployment

### Railway Deployment
The application is configured for Railway deployment with:
- **Dockerfile**: Multi-stage build with Playwright support for web scraping
- **railway.toml**: Railway-specific configuration
- **Health Check**: Available at `/health` endpoint
- **PostgreSQL**: Configured for Railway's managed PostgreSQL

### Environment Variables (Production)
```
DATABASE_HOST=<railway-postgres-host>
DATABASE_NAME=<railway-postgres-db>
DATABASE_USER=<railway-postgres-user>
DATABASE_PASSWORD=<railway-postgres-password>
DATABASE_PORT=5432
JWT_SECRET_KEY=<your-strong-jwt-secret>
RAPIDAPI_PINNACLE_KEY=<your-rapidapi-key>
```

### Deployment Commands
```bash
# Railway CLI deployment
railway login
railway link
railway up

# Or connect to Railway GitHub integration
git push origin main
```

## Security Considerations

- JWT secret should be environment-specific in production
- CORS policy needs to be restricted for production
- Database credentials should use environment variables
- API keys (RapidAPI) should be secured
- Playwright browsers run in sandboxed environment