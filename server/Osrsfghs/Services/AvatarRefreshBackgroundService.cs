using NetCord;
using NetCord.Gateway;
using Osrsfghs.Interfaces;
using Osrsfghs.Models;

namespace Osrsfghs.Services
{
    public class AvatarRefreshBackgroundService : BackgroundService
    {
        private readonly ILogger<AvatarRefreshBackgroundService> _logger;
        private readonly ITrackedCharacterStore _trackedCharacterStore;
        private readonly IHighScoreService _highScoreService;
        private readonly GatewayClient _gatewayClient;

        public AvatarRefreshBackgroundService(
            ILogger<AvatarRefreshBackgroundService> logger,
            ITrackedCharacterStore trackedCharacterStore,
            IHighScoreService highScoreService,
            GatewayClient gatewayClient)
        {
            _logger = logger;
            _trackedCharacterStore = trackedCharacterStore;
            _highScoreService = highScoreService;
            _gatewayClient = gatewayClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer trackedCharacterTimer = new(TimeSpan.FromSeconds(5));
            while(await trackedCharacterTimer.WaitForNextTickAsync(stoppingToken))
            {
                if(_trackedCharacterStore.GetTrackedCharacters()?.Count > 0)
                    break;
            }

            await RefreshAllAvatarsAsync(stoppingToken);

            using PeriodicTimer timer = new(TimeSpan.FromDays(1));

            try
            {
                while(await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await RefreshAllAvatarsAsync(stoppingToken);
                }
            }
            catch(OperationCanceledException)
            {
                _logger.LogInformation("AvatarRefreshBackgroundService execution stopping");
            }
        }

        private async Task RefreshAllAvatarsAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<Character> characters = _trackedCharacterStore.GetTrackedCharacters();

            foreach(Character character in characters)
            {
                if(string.IsNullOrWhiteSpace(character.DiscordUserId))
                    continue;

                try
                {
                    if(!ulong.TryParse(character.DiscordUserId, out ulong userId))
                        continue;

                    string? avatarUrl = TryGetCachedAvatarUrl(userId)
                        ?? (await _gatewayClient.Rest.GetUserAsync(userId, cancellationToken: cancellationToken))
                            .GetAvatarUrl()?.ToString();

                    if(avatarUrl is not null)
                    {
                        await _highScoreService.UpdateAvatarUrlAsync(character.Id, avatarUrl);
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh avatar for character {character}", character.Name);
                }
            }
        }

        private string? TryGetCachedAvatarUrl(ulong userId)
        {
            foreach(Guild? guild in _gatewayClient.Cache.Guilds.Values)
            {
                if(guild.Users.TryGetValue(userId, out GuildUser? guildUser))
                {
                    return (guildUser.GetGuildAvatarUrl() ?? guildUser.GetAvatarUrl())?.ToString();
                }
            }

            return null;
        }
    }
}
