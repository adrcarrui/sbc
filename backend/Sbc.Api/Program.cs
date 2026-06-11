using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Sbc.Api.BackgroundServices;
using Sbc.Application.Integrations.UrBackup;
using Sbc.Infrastructure.Integrations.UrBackup;
using Sbc.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<SbcDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SbcDb");

    options
        .UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention();
});

builder.Services.Configure<UrBackupOptions>(
    builder.Configuration.GetSection("UrBackup"));

builder.Services.AddHttpClient<IUrBackupClient, UrBackupClient>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<UrBackupOptions>>()
        .Value;

    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl);
    }

    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

builder.Services.AddScoped<IUrBackupClientSyncService, UrBackupClientSyncService>();

builder.Services.AddHostedService<UrBackupHealthWorker>();
builder.Services.AddHostedService<UrBackupClientSyncWorker>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();