using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SampleApp.WebUI.Data;
using Serilog;

var serviceName = "SampleApp";
var serviceVersion = "1.0.0";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Host.UseSerilog((_, lc) =>
{
    lc.Enrich.WithProperty("ServiceVersion", serviceName)
        .Enrich.WithProperty("ServiceVersion", serviceVersion)
        .WriteTo.Console()
        .WriteTo.Seq("http://host.docker.internal:5341")
        //.WriteTo.Seq("http://localhost:5341")
        ;
});

builder.Services.AddHttpLogging(options => options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All);
builder.Services.AddHealthChecks();

builder.Services.Configure<AspNetCoreInstrumentationOptions>(options =>
{
    options.Enrich = (activity, @event, @object) =>
    {
        if (@event.Equals("OnStartActivity"))
        {
            if (@object is HttpRequest httpRequest)
            {
                activity.SetTag("requestProtocol", httpRequest.Protocol);
            }
        }
        else if (@event.Equals("OnStopActivity"))
        {
            if (@object is HttpResponse httpResponse)
            {
                activity.SetTag("responseLength", httpResponse.ContentLength);
            }
        }
    };
    options.Filter = context => context.Request.Method.Equals("GET");
});

// Add services to the container.
builder.Services.AddOpenTelemetryTracing((builder) => builder
    .AddSource(serviceName)
    .SetResourceBuilder(ResourceBuilder
        .CreateDefault()
        .AddTelemetrySdk()
        .AddEnvironmentVariableDetector()
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .SetSampler(new AlwaysOnSampler())
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddSqlClientInstrumentation()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://otel-collector:4317");
        options.Protocol = OtlpExportProtocol.Grpc;
    }));

builder.Services.AddSingleton(TracerProvider.Default.GetTracer(serviceName));
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseHttpLogging();
app.UseHealthChecks("/health");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

await app.RunAsync();
