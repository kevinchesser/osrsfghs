using GoonHighScoresServer.Models;

namespace GoonHighScoresServer.Interfaces
{
    public interface IHighScoreRepository
    {
        Task<List<Character>> GetCharacters();
        Task<Dictionary<int, int>> GetMostRecentXpDrops(int characterId);
        Task<int?> GetMostRecentOverallXp(int characterId);
        Task SaveXpDrops(List<XpDrop> xpDrops, string processingTime);
        Task<Dictionary<int, CharacterLeaderboardEntry>> GetCharacterLeaderboardEntriesForOverallXp(DateTime backdatedDateTimeUtc);
        Task<List<XpDrop>> GetAllXpDropsAndFallbackIfNoXpDropWithinCutoff(int characterId, DateTime backdatedDateTimeUtc);
        Task<int> GetCharacterId(string characterName);
    }
}
