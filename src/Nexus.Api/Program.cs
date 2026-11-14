using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nexus.Api.Services;
using Nexus.Core.Services;
using Nexus.Data;

var builder = WebApplication.CreateBuilder(args);

// ---- Capa de datos ----
// Proveedor configurable: "SqlServer" (el definido en el informe) o "Sqlite" (arranque rápido sin instalar nada).
var proveedor = builder.Configuration["Database:Provider"] ?? "SqlServer";
builder.Services.AddDbContext<NexusDbContext>(o =>
{
    var cadena = builder.Configuration.GetConnectionString("Nexus");
    if (proveedor.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)) o.UseSqlite(cadena);
    else o.UseSqlServer(cadena);
});

// ---- Servicios de negocio (Nexus.Core) y de aplicación (Nexus.Api) ----
builder.Services.AddSingleton<PrioridadService>();
builder.Services.AddSingleton<RecomendacionService>();
builder.Services.AddSingleton<DetectorCoincidencias>();
builder.Services.AddSingleton<SimuladorService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<IncidenteAppService>();
builder.Services.AddScoped<SimulacionAppService>();

// ---- Autenticación JWT: los permisos se validan en el servidor (RNF001) ----
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException(
        "Configure Jwt:Key (mínimo 32 caracteres) con 'dotnet user-secrets' o una variable de entorno.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NEXUS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Pegue el token obtenido en /api/auth/login",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Crea la base y carga datos sintéticos de demostración (solo en desarrollo).
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NexusDbContext>();
    db.Database.EnsureCreated();
    DataSeeder.Sembrar(db);
}

app.UseDefaultFiles();   // sirve wwwroot/index.html (interfaz web)
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
