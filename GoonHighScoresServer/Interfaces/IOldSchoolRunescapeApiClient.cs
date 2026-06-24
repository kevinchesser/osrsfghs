using GoonHighScoresServer.Models;

namespace GoonHighScoresServer.Interfaces
{
    public interface IOldSchoolRunescapeApiClient
    {
        Task<OsrsCharacterStats> GetOsrsCharacterStats(string characterName);
    }
}
