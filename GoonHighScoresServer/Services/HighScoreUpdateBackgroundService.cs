using GoonHighScoresServer.Interfaces;
using GoonHighScoresServer.Models;
using Microsoft.Extensions.Options;

namespace GoonHighScoresServer.Services
{
    public class HighScoreUpdateBackgroundService : BackgroundService
    {
        private readonly ILogger<HighScoreUpdateBackgroundService> _logger;
        private readonly HighScoreUpdateBackgroundServiceOptions _options;
        private readonly IOldSchoolRunescapeApiClient _oldSchoolRunescapeApiClient;
        private readonly ITrackedCharacterStore _trackedCharacterStore;
        private readonly IHighScoreService _highScoreService;
        private int _executionCount;

        public HighScoreUpdateBackgroundService(ILogger<HighScoreUpdateBackgroundService> logger, IOptions<HighScoreUpdateBackgroundServiceOptions> options,
            IOldSchoolRunescapeApiClient oldSchoolRunescapeApiClient, ITrackedCharacterStore trackedCharacterStore, IHighScoreService highScoreService)
        {
            _logger = logger;
            _options = options.Value;
            _oldSchoolRunescapeApiClient = oldSchoolRunescapeApiClient;
            _trackedCharacterStore = trackedCharacterStore;
            _highScoreService = highScoreService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("HighScoreUpdateBackgroundService execution started");

            if(!_options.Enabled)
                return;

            using PeriodicTimer trackedCharacterTimer = new(TimeSpan.FromSeconds(5));
            while(await trackedCharacterTimer.WaitForNextTickAsync(stoppingToken))
            {
                if(_trackedCharacterStore.GetTrackedCharacters()?.Count > 0)
                    break;
            }

            await DoWorkAsync();

            using PeriodicTimer timer = new(TimeSpan.FromMinutes(30));

            try
            {
                while(await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await DoWorkAsync();
                }
            }
            catch(OperationCanceledException)
            {
                _logger.LogInformation("HighScoreUpdateBackgroundService execution stopping");
            }

            async Task DoWorkAsync()
            {
                string processingTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"); //ISO-8601 format
                foreach(Character character in _trackedCharacterStore.GetTrackedCharacters())
                {
                    try
                    {
                        await ProcessHighScoresForCharacter(character, processingTime);
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "Exception when processing highscores for {character}", character.Name);
                    }
                }
            }

        }

        private async Task ProcessHighScoresForCharacter(Character character, string processingTime)
        {
            int count = Interlocked.Increment(ref _executionCount);
            _logger.LogInformation("ProcessHighScoresForCharacter: {name} {executionCount}", character.Name, count);

            OsrsCharacterStats osrsCharacterStats = await _oldSchoolRunescapeApiClient.GetOsrsCharacterStats(character.Name);
            Dictionary<int, OsrsSkill> osrsCharacterStatDictionary = osrsCharacterStats.Skills.ToDictionary(x => x.Id, y => y);
            await _highScoreService.RecordXpDropsIfNecessary(character, osrsCharacterStatDictionary, processingTime);
        }

        public class HighScoreUpdateBackgroundServiceOptions
        {
            public bool Enabled { get; set; }
        }
    }
}
