using Microsoft.EntityFrameworkCore;
using OpenDataTables.AspNetCore;
using OpenSelect2.AspNetCore;
using OpenSelect2.AspNetCore.Abstractions;
using SampleApp;
using SampleApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SampleDbContext>(o => o.UseInMemoryDatabase("SampleApp"));

// OpenSelect2 — options are optional; defaults apply without this call.
builder.Services.AddOpenSelect2(options =>
{
    options.DefaultLimit = 10;
    options.LoginUrl = "/Home/Index";
    options.Localization = Select2Localization.English;
});

// Host-implemented preselection hook (replaces the package's old claim-based logic).
builder.Services.AddScoped<ISelect2Preselector, SamplePreselector>();

builder.Services.AddOpenDataTables(options =>
{
    options.DefaultPageLength = 10;
    options.LoginUrl = "/Home/Index";
    options.Localization = DataTableLocalization.English;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    SampleDataSeeder.Seed(scope.ServiceProvider.GetRequiredService<SampleDbContext>());
}

app.UseStaticFiles();
app.UseRouting();
app.MapDefaultControllerRoute();

app.Run();

// Exposed so the WebApplicationFactory in the test project can boot this app.
public partial class Program { }
