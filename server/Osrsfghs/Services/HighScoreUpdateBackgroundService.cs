using Osrsfghs.Interfaces;
using Osrsfghs.Models;
using Microsoft.Extensions.Options;

namespace Osrsfghs.Services
{
    public class HighScoreUpdateBackgroundService : BackgroundService
    {
        private readonly ILogger<HighScoreUpdateBackgroundService> _logger;
        private readonly HighScoreUpdateBackgroundServiceOptions _options;
        private readonly ITrackedCharacterStore _trackedCharacterStore;
        private readonly IHighScoreService _highScoreService;
        private int _executionCount;

        public HighScoreUpdateBackgroundService(ILogger<HighScoreUpdateBackgroundService> logger, IOptions<HighScoreUpdateBackgroundServiceOptions> options,
            ITrackedCharacterStore trackedCharacterStore, IHighScoreService highScoreService)
        {
            _logger = logger;
            _options = options.Value;
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
                using SemaphoreSlim semaphore = new SemaphoreSlim(initialCount: 5);

                IEnumerable<Task> tasks = _trackedCharacterStore.GetTrackedCharacters()
                    .Select(character => ProcessCharacterSafeAsync(character, processingTime, semaphore));

                await Task.WhenAll(tasks);
            }

            async Task ProcessCharacterSafeAsync(Character character, string processingTime, SemaphoreSlim semaphore)
            {
                await semaphore.WaitAsync();
                try
                {
                    await ProcessHighScoresForCharacter(character, processingTime);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Exception when processing highscores for {character}", character.Name);
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        private async Task ProcessHighScoresForCharacter(Character character, string processingTime)
        {
            int count = Interlocked.Increment(ref _executionCount);
            _logger.LogInformation("ProcessHighScoresForCharacter: {name} {executionCount}", character.Name, count);
            await _highScoreService.ProcessHighScoresForCharacter(character, processingTime);
        }

        public class HighScoreUpdateBackgroundServiceOptions
        {
            public bool Enabled { get; set; }
        }
    }
}
