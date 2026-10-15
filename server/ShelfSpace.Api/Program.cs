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
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 bytes. Set it via the Jwt__Key environment variable " +
        "or, in Development, `dotnet user-secrets set \"Jwt:Key\" \"<random 32+ byte string>\"` " +
        "from server/ShelfSpace.Api.");
}

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
        // apply to it. Browser origins (a future web client, a testing tool)
        // must be listed in Cors:AllowedOrigins; with none configured, no
        // cross-origin browser access is allowed.
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
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
