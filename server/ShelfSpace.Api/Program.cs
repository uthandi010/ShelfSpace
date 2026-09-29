using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShelfSpace.Api.Auth;
using ShelfSpace.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// ----- Services -----
// Note: OrderStatus is converted to a plain string (order.Status.ToString())
// in OrdersController's DTO mapping rather than serialized as a raw enum, so
// no JsonStringEnumConverter is needed here.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=shelfspace.db";
builder.Services.AddDbContext<ShelfSpaceContext>(options => options.UseSqlite(connectionString));

builder.Services.AddSingleton<TokenService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

const string ClientCorsPolicy = "AllowClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
    {
        // The MAUI desktop client isn't a browser origin, so CORS doesn't
        // apply to it - this stays wide open mainly so a browser-based API
        // testing tool (or a future web client) can also reach the API
        // during local development.
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Applies pending migrations automatically so `dotnet run` on a fresh clone
// just works without a separate `dotnet ef database update` step.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShelfSpaceContext>();
    db.Database.Migrate();
}

app.UseCors(ClientCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory<Program> in the integration tests.
public partial class Program { }
