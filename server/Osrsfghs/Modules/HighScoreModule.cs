using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Osrsfghs.Interfaces;
using Osrsfghs.Models;

namespace Osrsfghs.Modules
{
    public class HighScoreModule : ApplicationCommandModule<ApplicationCommandContext>
    {
        private readonly IHighScoreService _highScoreService;
        private readonly ITrackedCharacterStore _trackedCharacterStore;
        private readonly static Dictionary<string, DateTime> _characterUpdateRateLimit = new Dictionary<string, DateTime>();
        private readonly static NetCord.Color vididOrange = new NetCord.Color(255, 142, 74); //Vivid orange

        public HighScoreModule(IHighScoreService highScoreService, ITrackedCharacterStore trackedCharacterStore,
            IOldSchoolRunescapeApiClient oldSchoolRunescapeApiClient)
        {
            _highScoreService = highScoreService;
            _trackedCharacterStore = trackedCharacterStore;
        }

        [SlashCommand("highscores", "Gets the tracked users with xp gain in the last 24 hrs")]
        public async Task Last24HrsHighscoresAsync()
        {
            TimespanXpLeaderboardViewModel timespanXpLeaderboardViewModel =
                await _highScoreService.GetLastXTimeSpanOverallXpLeadboard(TimeSpan.FromHours(24));
            EmbedProperties embed = new EmbedProperties();
            embed.Color = vididOrange;

            if (timespanXpLeaderboardViewModel.TimeSpanLeaderboardItems != null)
            {
                embed.Title = "Xp gained in the last 24hrs";
                int index = 1;

                foreach (TimeSpanLeaderboardItem timeSpanLeaderboardItem in timespanXpLeaderboardViewModel.TimeSpanLeaderboardItems)
                {
                    EmbedFieldProperties fieldProperties = new EmbedFieldProperties()
                        .WithName($"{index}. {timeSpanLeaderboardItem.Character.Name}")
                        .WithValue($"{timeSpanLeaderboardItem.TimeSpanOverallXp:N0}")
                        .WithInline(true);
                    embed.AddFields(fieldProperties);
                    index++;
                }
            }
            else
            {
                embed.Title = "No Xp gained in the last 24 hrs. osrs is dead";
            }

            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([embed])));
        }

        [SlashCommand("update", "Update stats with a 5 minute cooldown")]
        public async Task UpdateOsrsCharacterStats(
            [SlashCommandParameter(Name = "name", Description = "Name of the character you want to fetch stats for")] string characterName)
        {
            IReadOnlyList<Character> trackedCharacterStore = _trackedCharacterStore.GetTrackedCharacters();
            IEnumerable<Character> characterEnumerable = trackedCharacterStore.Where(x => x.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));
            if (characterEnumerable.Count() != 1)
            {
                await Context.Interaction.SendResponseAsync(
                   InteractionCallback.Message(new InteractionMessageProperties().WithContent("Not tracking that character")));
                return;
            }

            DateTime processingDateTimeUtc =  DateTime.UtcNow;
            if (_characterUpdateRateLimit.TryGetValue(characterName, out DateTime cooldownTime))
            {
                if (processingDateTimeUtc < cooldownTime)
                {
                    await Context.Interaction.SendResponseAsync(
                        InteractionCallback.Message(new InteractionMessageProperties().WithContent("Already updated within the last 5 minutes")));
                    return;
                }
            }

            Character targetCharacter = characterEnumerable.First();
            string processingTime = processingDateTimeUtc.ToString("O"); //ISO-8601 format
            await _highScoreService.ProcessHighScoresForCharacter(targetCharacter, processingTime);
            _characterUpdateRateLimit[characterName] = processingDateTimeUtc.AddMinutes(5);
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new InteractionMessageProperties().WithContent($"{characterName}'s stats fetched")));
            return;
        }
    }
}
