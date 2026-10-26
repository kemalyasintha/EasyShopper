using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using StackExchange.Redis;
using TechTalk.SpecFlow;
using static Microsoft.Playwright.Assertions;

namespace EasyShopper.E2E;

[Binding]
public sealed class ShoppingSteps
{
    private readonly ScenarioContext scenario;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:6355";
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly string id = ObjectId.GenerateNewId().ToString();
    private readonly string name = "E2E-" + Guid.NewGuid().ToString("N");
    private string user = "e2e-" + Guid.NewGuid().ToString("N");
    private IPlaywright? playwright;
    private IBrowser? browser;
    private IBrowserContext? context;
    private IPage page = null!;
    private ConnectionMultiplexer? redis;
    private IMongoCollection<BsonDocument> Products => new MongoClient(
        Environment.GetEnvironmentVariable("E2E_MONGO") ?? "mongodb://localhost:27017")
        .GetDatabase("Product").GetCollection<BsonDocument>("product");
    public ShoppingSteps(ScenarioContext scenario) { this.scenario = scenario; }
    private async Task<IDatabase> Redis()
    {
        redis ??= await ConnectionMultiplexer.ConnectAsync(Environment.GetEnvironmentVariable("E2E_REDIS") ?? "localhost:6379");
        return redis.GetDatabase();
    }
    private static async Task Eventually(Func<Task<bool>> check, string description)
    {
        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (DateTime.UtcNow < deadline)
        {
            if (await check()) return;
            await Task.Delay(1000);
        }
        Assert.Fail("Timed out waiting for " + description);
    }
    [Given("I submit a unique product through the gateway")]
    public async Task CreateProduct()
    {
        using var response = await http.PostAsJsonAsync(baseUrl + "/api/product/add", new {
            productId = id, productName = name, productDescription = "BDD fixture", productPrice = 12.5,
            categoryId = Guid.NewGuid() });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await response.Content.ReadAsStringAsync());
    }
    [Then("the exact product eventually exists in MongoDB")]
    public Task MongoProduct() => Eventually(async () => {
        var doc = await Products.Find(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id))).FirstOrDefaultAsync();
        return doc != null && doc["productName"].AsString == name && doc["productPrice"].ToDouble() == 12.5;
    }, "MongoDB product " + id);
    [Given("the gateway eventually returns that product")]
    [Then("the gateway eventually returns that product")]
    public Task GatewayProduct() => Eventually(async () => {
        using var response = await http.GetAsync(baseUrl + "/api/product/" + id);
        if (response.StatusCode == HttpStatusCode.NotFound || (int)response.StatusCode == 429) return false;
        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<JsonElement>();
        return value.GetProperty("productId").GetString() == id && value.GetProperty("productName").GetString() == name;
    }, "gateway product " + id);
    [When("I submit a cart containing that product through the gateway")]
    public async Task CreateCart()
    {
        using var response = await http.PostAsJsonAsync(baseUrl + "/api/cart/add", new {
            cartId = Guid.NewGuid().ToString(), userId = user, amount = 12.5m,
            items = new[] { new { productId = id, quantity = 1, price = 12.5m } } });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
    }
    [Then("the exact cart eventually exists in Redis")]
    public async Task RedisCart()
    {
        var db = await Redis();
        await Eventually(async () => {
            var raw = await db.StringGetAsync(user);
            if (raw.IsNullOrEmpty) return false;
            using var json = JsonDocument.Parse(raw.ToString());
            var cart = json.RootElement;
            var item = cart.GetProperty("Items")[0];
            return cart.GetProperty("UserId").GetString() == user && cart.GetProperty("Items").GetArrayLength() == 1
                && item.GetProperty("ProductId").GetString() == id && item.GetProperty("Quantity").GetInt32() == 1;
        }, "Redis cart " + user);
    }
    [Then("the gateway eventually returns that cart")]
    public Task GatewayCart() => Eventually(async () => {
        using var response = await http.GetAsync(baseUrl + "/api/cart/" + user);
        response.EnsureSuccessStatusCode();
        var cart = await response.Content.ReadFromJsonAsync<JsonElement>();
        return cart.GetProperty("userId").GetString() == user && cart.GetProperty("items")[0].GetProperty("productId").GetString() == id;
    }, "gateway cart " + user);
    [Given("I open the EasyShopper storefront")]
    public async Task Open()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync();
        context = await browser.NewContextAsync();
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl);
    }
    [When("I select French")]
    public Task French() => page.GetByRole(AriaRole.Button, new() { Name = "Français", Exact = true }).ClickAsync();
    [When("I select English")]
    public Task English() => page.GetByRole(AriaRole.Button, new() { Name = "English", Exact = true }).ClickAsync();
    [When("I reload the storefront")]
    public async Task Reload() { await page.ReloadAsync(); }
    private async Task Controls(string language, string heading, string label, string search)
    {
        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", language);
        await Expect(page.GetByTestId("heading")).ToHaveTextAsync(heading);
        await Expect(page.GetByTestId("product-label")).ToHaveTextAsync(label);
        await Expect(page.GetByTestId("search")).ToHaveTextAsync(search);
    }
    [Then("the French shopping controls are displayed")]
    public Task FrenchControls() => Controls("fr", "Acheter des produits", "Identifiant du produit", "Rechercher un produit");
    [Then("the English shopping controls are displayed")]
    public Task EnglishControls() => Controls("en", "Shop products", "Product ID", "Find product");
    [When("I find my product and add it to the cart")]
    public async Task Shop()
    {
        await page.GetByLabel("Identifiant du produit", new() { Exact = true }).FillAsync(id);
        await page.GetByTestId("search").ClickAsync();
        await Expect(page.GetByTestId("product-name")).ToHaveTextAsync(name);
        await Expect(page.GetByTestId("add")).ToHaveTextAsync("Ajouter au panier");
        await page.GetByTestId("add").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Ajouté au panier");
        user = await page.EvaluateAsync<string>("localStorage.getItem('shopper')");
    }
    [Then("the gateway rejects an invalid product identifier")]
    public async Task Invalid()
    {
        using var response = await http.GetAsync(baseUrl + "/api/product/not-an-object-id");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    [AfterScenario]
    public async Task Cleanup()
    {
        try
        {
            if (context != null)
            {
                var directory = Environment.GetEnvironmentVariable("E2E_ARTIFACTS") ?? "TestResults";
                Directory.CreateDirectory(directory);
                var prefix = Path.Combine(directory, Guid.NewGuid().ToString("N"));
                if (scenario.TestError != null) await page.ScreenshotAsync(new() { Path = prefix + ".png", FullPage = true });
                await context.Tracing.StopAsync(new() { Path = prefix + ".zip" });
            }
        }
        finally
        {
            if (browser != null) await browser.DisposeAsync();
            playwright?.Dispose();
            try
            {
                await Products.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id)));
                var db = await Redis();
                await db.KeyDeleteAsync(user);
            }
            finally { redis?.Dispose(); http.Dispose(); }
        }
    }
}
