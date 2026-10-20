using DescopeSampleApp.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient(TokenValidator.JwksClientName);
builder.Services.AddTransient(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var projectId = configuration["DESCOPE_PROJECT_ID"] ?? "P2dI0leWLEC45BDmfxeOCSSOWiCt";
    var url = configuration["Descope:JwksUrl"] ?? $"https://api.descope.com/{Uri.EscapeDataString(projectId)}/.well-known/jwks.json";
    return new TokenValidator(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(TokenValidator.JwksClientName),
        new Uri(url, UriKind.Absolute),
        sp.GetRequiredService<TimeProvider>());
});
builder.Services.AddSingleton(TimeProvider.System);
var app = builder.Build();

app.UseStaticFiles();
app.MapControllers();
app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

// Make the minimal-hosting entry point accessible to WebApplicationFactory integration tests.
public partial class Program { }
