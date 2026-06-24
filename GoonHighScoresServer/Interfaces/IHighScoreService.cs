using GoonHighScoresServer.Models;

namespace GoonHighScoresServer.Interfaces
{
    public interface IHighScoreService
    {
        Task<List<Character>> GetCharacters();
        Task RecordXpDropsIfNecessary(Character character, Dictionary<int, OsrsSkill> osrsCharacterStats, string processingTime);
        /// <summary>
        /// Look back x amount of time for xp gains to build a leaderboard for overall xp gains over a period of time 
        /// </summary>
        /// <param name="lookbackTimeSpan">How far back to look for xp gains</param>
        /// <returns></returns>
        Task<TimespanXpLeaderboardViewModel> GetLastXTimeSpanOverallXpLeadboard(TimeSpan lookbackTimeSpan);
        Task<CharacterOverview> GetCharacterOverview(string characterName);
    }
}
