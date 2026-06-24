using GoonHighScoresServer.Interfaces;
using GoonHighScoresServer.Repositories;
using GoonHighScoresServer.Services;
using Microsoft.OpenApi;
using static GoonHighScoresServer.Repositories.HighScoreSQLiteRepository;
using static GoonHighScoresServer.Services.HighScoreUpdateBackgroundService;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;
IConfiguration configuration = builder.Configuration;
string goonHighScoresCorsPolicyName = "_goonHighScoresSpecificOrigins";

ConfigureControllers();
ConfigureCors();
ConfigureDatabases();
ConfigureServices();
ConfigureHostedServices();
ConfigureHttpClients();

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
        options.AddPolicy(name: goonHighScoresCorsPolicyName,
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

app.MapSwagger();
app.UseSwagger();
app.UseSwaggerUI(options => {
    options.SwaggerEndpoint("v1/swagger.json", "HighScores API");
});

app.UseCors(goonHighScoresCorsPolicyName);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
