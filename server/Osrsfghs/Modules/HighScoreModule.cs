using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Osrsfghs.Interfaces;
using Osrsfghs.Models;

namespace Osrsfghs.Modules
{
    public class HighScoreModule : ApplicationCommandModule<ApplicationCommandContext>
    {
        private readonly IHighScoreService _highScoreService;

        public HighScoreModule(IHighScoreService highScoreService)
        {
            _highScoreService = highScoreService;
        }

        [SlashCommand("highscores", "Gets the tracked users with xp gain in the last 24 hrs")]
        public async Task Last24HrsHighscoresAsync()
        {
            TimespanXpLeaderboardViewModel timespanXpLeaderboardViewModel =
                await _highScoreService.GetLastXTimeSpanOverallXpLeadboard(TimeSpan.FromHours(24));
            EmbedProperties embed = new EmbedProperties()
                .WithTitle("XP Gained in the last 24hrs");

            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([embed])));
        }
    }
}
