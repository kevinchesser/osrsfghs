using GoonHighScoresServer.Interfaces;
using GoonHighScoresServer.Models;

namespace GoonHighScoresServer.Services
{
    public class TrackedCharacterBackgroundService : BackgroundService
    {
        private readonly ILogger<TrackedCharacterBackgroundService> _logger;
        private readonly IHighScoreService _highScoreService;
        private readonly ITrackedCharacterStore _trackedCharacterStore;
        private int _executionCount;

        public TrackedCharacterBackgroundService(ILogger<TrackedCharacterBackgroundService> logger, IHighScoreService highScoreService,
            ITrackedCharacterStore trackedCharacterStore)
        {
            _logger = logger;
            _highScoreService = highScoreService;
            _trackedCharacterStore = trackedCharacterStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TrackedCharacterBackgroundService execution started");

            //Get tracked characters on startup
            await GetAndStoreTrackedCharacters();

            using PeriodicTimer timer = new(TimeSpan.FromMinutes(1));
            try
            {
                while(await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await GetAndStoreTrackedCharacters();
                }
            }
            catch(OperationCanceledException)
            {
                _logger.LogInformation("TrackedCharacterBackgroundService execution stopping");
            }

        }

        private async Task GetAndStoreTrackedCharacters()
        {
            int count = Interlocked.Increment(ref _executionCount);

            List<Character> trackedCharacters = await _highScoreService.GetCharacters();
            _trackedCharacterStore.SetTrackedCharacters(trackedCharacters);

            _logger.LogInformation("TrackedCharacterBackgroundService : {ExecutionCount}", count);
        }
    }
}
