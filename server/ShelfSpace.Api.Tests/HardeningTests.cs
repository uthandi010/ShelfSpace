using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShelfSpace.Api.Auth;
using ShelfSpace.Api.Data;
using ShelfSpace.Api.Dtos;
using Xunit;

namespace ShelfSpace.Api.Tests;

public class HardeningTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HardeningTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static string LegacyHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"100000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static StringContent EmptyJson() => new("{}", Encoding.UTF8, "application/json");

    [Theory]
    [InlineData("")]
    [InlineData("short-key")]
    public void Startup_WithMissingOrShortJwtKey_Fails(string key)
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("Jwt:Key", key));

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Jwt:Key", ex.ToString());
    }

    [Fact]
    public async Task Register_WithEmptyBody_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/auth/register", EmptyJson());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyBody_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/auth/login", EmptyJson());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("Bad Email", "not-an-email", "Password123!"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithOverlongPasswordOrName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var longPassword = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("Ok", "longpw@example.com", new string('a', 129)));
        var longName = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(new string('n', 101), "longname@example.com", "Password123!"));
        Assert.Equal(HttpStatusCode.BadRequest, longPassword.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longName.StatusCode);
    }

    [Fact]
    public void PasswordHasher_VerifiesLegacyHashes_AndFlagsThemForRehash()
    {
        var legacy = LegacyHash("Password123!");

        Assert.True(PasswordHasher.Verify("Password123!", legacy));
        Assert.False(PasswordHasher.Verify("wrong", legacy));
        Assert.True(PasswordHasher.NeedsRehash(legacy));

        var fresh = PasswordHasher.Hash("Password123!");
        Assert.StartsWith("600000.", fresh);
        Assert.True(PasswordHasher.Verify("Password123!", fresh));
        Assert.False(PasswordHasher.NeedsRehash(fresh));
    }

    [Fact]
    public async Task Login_WithLegacyHash_SucceedsAndUpgradesHash()
    {
        var client = _factory.CreateClient();
        var auth = await client.RegisterAsync(email: "legacy@example.com", password: "Password123!");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShelfSpaceContext>();
            var user = await db.Users.SingleAsync(u => u.Id == auth.UserId);
            user.PasswordHash = LegacyHash("Password123!");
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("legacy@example.com", "Password123!"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ShelfSpaceContext>();
        var upgraded = await verifyDb.Users.AsNoTracking().SingleAsync(u => u.Id == auth.UserId);
        Assert.StartsWith("600000.", upgraded.PasswordHash);
    }

    private async Task<(HttpClient Client, ProductResponse Product)> NewProductAsync(string sku, int quantity)
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());
        var response = await client.PostAsJsonAsync(
            "/api/products", new CreateProductRequest(sku, "Name " + sku, null, 100, quantity, null));
        response.EnsureSuccessStatusCode();
        return (client, (await response.Content.ReadFromJsonAsync<ProductResponse>())!);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10_001)]
    public async Task CreatingOrder_WithOutOfRangeQuantity_ReturnsBadRequest(int quantity)
    {
        var (client, product) = await NewProductAsync("QTY-1", 5);

        var response = await client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest("Jane", new List<CreateOrderItemRequest> { new(product.Id, quantity) }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatingOrder_WithDuplicateLinesExceedingMax_ReturnsBadRequest()
    {
        var (client, product) = await NewProductAsync("QTY-2", 5);

        var response = await client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest("Jane", new List<CreateOrderItemRequest>
            {
                new(product.Id, 10_000),
                new(product.Id, 10_000),
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Restock_ThatWouldOverflowInt_ReturnsBadRequest()
    {
        var (client, product) = await NewProductAsync("OVF-1", int.MaxValue);

        var response = await client.PostAsJsonAsync(
            $"/api/products/{product.Id}/restock", new RestockRequest(1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StaleProductWrite_ThrowsDbUpdateConcurrencyException()
    {
        var (_, product) = await NewProductAsync("CC-1", 10);

        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<ShelfSpaceContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<ShelfSpaceContext>();
        var a = await dbA.Products.SingleAsync(p => p.Id == product.Id);
        var b = await dbB.Products.SingleAsync(p => p.Id == product.Id);

        a.QuantityOnHand -= 1;
        await dbA.SaveChangesAsync();
        b.QuantityOnHand -= 1;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
    }

    [Fact]
    public async Task CreatingProduct_WithNegativeThresholdOrOverlongFields_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.AuthorizeAs(await client.RegisterAsync());

        var negative = await client.PostAsJsonAsync(
            "/api/products", new CreateProductRequest("V-1", "Name", null, 100, 1, -1));
        var longSku = await client.PostAsJsonAsync(
            "/api/products", new CreateProductRequest(new string('s', 65), "Name", null, 100, 1, null));
        var longName = await client.PostAsJsonAsync(
            "/api/products", new CreateProductRequest("V-2", new string('n', 201), null, 100, 1, null));
        var longDescription = await client.PostAsJsonAsync(
            "/api/products", new CreateProductRequest("V-3", "Name", new string('d', 2001), 100, 1, null));

        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longSku.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longName.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longDescription.StatusCode);
    }

    [Fact]
    public async Task UpdatingProduct_WithInvalidValues_ReturnsBadRequest()
    {
        var (client, product) = await NewProductAsync("U-1", 1);

        var negativePrice = await client.PatchAsJsonAsync(
            $"/api/products/{product.Id}", new UpdateProductRequest(null, null, -5, null));
        var longName = await client.PatchAsJsonAsync(
            $"/api/products/{product.Id}", new UpdateProductRequest(new string('n', 201), null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, negativePrice.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longName.StatusCode);
    }

    [Fact]
    public async Task Cors_AllowsOnlyConfiguredOrigins()
    {
        var client = _factory.CreateClient();

        var allowed = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        allowed.Headers.Add("Origin", "https://allowed.example");
        var allowedResponse = await client.SendAsync(allowed);

        var denied = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        denied.Headers.Add("Origin", "https://evil.example");
        var deniedResponse = await client.SendAsync(denied);

        Assert.True(allowedResponse.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(deniedResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
