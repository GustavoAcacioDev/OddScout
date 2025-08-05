using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using odd_scout.Middleware;
using OddScout.API.Common.Exceptions;
using OddScout.Application;
using OddScout.Infrastructure;
using OddScout.Infrastructure.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args); 

// Add services to the container
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OddScout API",
        Version = "v1",
        Description = "API for OddScout - Sports Betting Odds Comparison Platform"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// Add Application Layer (MediatR, FluentValidation)
builder.Services.AddApplication();

// Add Infrastructure Layer (DbContext, Services)
builder.Services.AddInfrastructure(builder.Configuration);

// Add JWT Authentication - CORRIGIDO
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? builder.Configuration["Jwt:Key"]!)),
        ValidateLifetime = true, // IMPORTANTE: Validar expira��o
        ClockSkew = TimeSpan.FromMinutes(5), // Toler�ncia de 5 minutos para diferen�as de rel�gio
        RequireExpirationTime = true,
        // Mapear claims corretamente
        NameClaimType = "name",
        RoleClaimType = "role"
    };

    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true; // MUDADO: Salvar token para debugging
    options.IncludeErrorDetails = true; // MUDADO: Incluir detalhes de erro para debugging

    // Event handlers para debugging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validated for: {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"OnChallenge error: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Add Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-migrate database on startup (for Railway deployment) - DISABLED FOR NOW
// Uncomment when needed or use /migration/run endpoint instead
/*
if (app.Environment.IsProduction())
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            logger.LogInformation("Starting database migration...");
            
            // Set timeout for migrations
            context.Database.SetCommandTimeout(120); // 2 minutes
            
            await context.Database.MigrateAsync();
            
            logger.LogInformation("Database migration completed successfully");
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database migration failed. Application will continue without migrations.");
        // Don't throw - let the app start even if migration fails
    }
}
*/

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseRouting();

app.UseApiResponseWrapping();

app.UseAuthentication(); // DEVE vir antes de UseAuthorization
app.UseAuthorization();

app.UseExceptionHandler();

app.MapControllers();

app.Run();