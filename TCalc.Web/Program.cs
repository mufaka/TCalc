using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TCalc.Web.Data;
using TCalc.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddSingleton<ICalculatorEngine, CalculatorEngine>();
builder.Services.AddSingleton<IGraphingService, GraphingService>();
builder.Services.AddSingleton<IGeometryService, GeometryService>();
builder.Services.AddSingleton<IStatisticsService, StatisticsService>();
builder.Services.AddSingleton<IRegressionService, RegressionService>();
builder.Services.AddSingleton<IProbabilityService, ProbabilityService>();
builder.Services.AddSingleton<ISimulationService, SimulationService>();
builder.Services.AddSingleton<IInferenceService, InferenceService>();
builder.Services.AddSingleton<ITimeSeriesService, TimeSeriesService>();
builder.Services.AddScoped<IDataSetService, DataSetService>();

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
