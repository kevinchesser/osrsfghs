using Osrsfghs.Interfaces;
using Osrsfghs.Repositories;
using Osrsfghs.Services;
using Microsoft.OpenApi;
using static Osrsfghs.Repositories.HighScoreSQLiteRepository;
using static Osrsfghs.Services.HighScoreUpdateBackgroundService;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using Osrsfghs.Modules;
using NetCord.Hosting.Services.ApplicationCommands;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;
IConfiguration configuration = builder.Configuration;
string osrsfghsCorsPolicyName = "_osrsfghsSpecificOrigins";

ConfigureControllers();
ConfigureCors();
ConfigureDatabases();
ConfigureServices();
ConfigureHostedServices();
ConfigureHttpClients();
ConfigureDiscordServices();

void ConfigureControllers()
{
    services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
    });
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "HighScores API", Version = "v1" });
    });
}

void ConfigureCors()
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: osrsfghsCorsPolicyName,
            policy =>
            {
                policy.WithOrigins(configuration.GetSection("AllowedOrigins").Value ?? string.Empty);
            });
    });
}

void ConfigureDatabases()
{
    services.Configure<HighScoreSQLiteRepositoryOptions>(configuration.GetSection("Databases:SQLite"));
    services.AddSingleton<IHighScoreRepository, HighScoreSQLiteRepository>();
}

void ConfigureServices()
{
    services.AddSingleton<IHighScoreService, HighScoreService>();
    services.AddSingleton<ITrackedCharacterStore, TrackedCharacterStore>();
}

void ConfigureHostedServices()
{
    services.AddHostedService<TrackedCharacterBackgroundService>();
    services.AddHostedService<HighScoreUpdateBackgroundService>();
    services.Configure<HighScoreUpdateBackgroundServiceOptions>(configuration.GetSection("HighScoreUpdateBackgroundService"));
    services.AddHostedService<AvatarRefreshBackgroundService>();
}

void ConfigureHttpClients()
{
    services.AddHttpClient<IOldSchoolRunescapeApiClient, OldSchoolRunescapeApiClient>(client =>
    {
        string baseUrl = configuration.GetSection("OldschoolRunescape:BaseUrl").Value;
        if (baseUrl == null)
            throw new ArgumentNullException("Osrs BaseUrl missing from appsettings");

        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });
}

void ConfigureDiscordServices()
{
    services.AddDiscordGateway().AddApplicationCommands();
}

builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "O";
    options.UseUtcTimestamp = true;
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.AddModules(typeof(HighScoreModule).Assembly);
app.MapSwagger();
app.UseSwagger();
app.UseSwaggerUI(options => {
    options.SwaggerEndpoint("v1/swagger.json", "HighScores API");
});

app.UseCors(osrsfghsCorsPolicyName);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//app.Run();
await app.RunAsync();
