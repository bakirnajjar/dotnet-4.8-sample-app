var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Run();

// Make the minimal-hosting entry point accessible to WebApplicationFactory integration tests.
public partial class Program { }
