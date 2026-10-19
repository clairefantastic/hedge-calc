using System.Text.Json.Serialization;
using HedgeCalc.Api.Data;
using HedgeCalc.Api.Domain;
using HedgeCalc.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Services (the DI container, like Spring's ApplicationContext) ----
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums as "SellForeign" instead of 1.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Lets clients send amounts as strings ("1000000.05") so JavaScript never
        // has to turn them into floating-point numbers first.
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<HedgeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("HedgeDb")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IHedgeCalculator, HedgeCalculator>();
builder.Services.AddScoped<IHedgeService, HedgeService>();

var app = builder.Build();

// ---- HTTP pipeline (middleware, like servlet filters) ----
app.UseExceptionHandler();   // unhandled exceptions -> RFC 7807 ProblemDetails, no stack traces leaked
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply EF Core migrations on startup (fine for a demo; in production you'd run them in the deploy pipeline).
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<HedgeDbContext>().Database.Migrate();
}

app.MapControllers();

app.Run();

// Makes the auto-generated Program class visible to WebApplicationFactory<Program> in the test project.
public partial class Program { }
