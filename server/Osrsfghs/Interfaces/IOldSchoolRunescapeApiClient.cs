using Osrsfghs.Models;

namespace Osrsfghs.Interfaces
{
    public interface IOldSchoolRunescapeApiClient
    {
        Task<OsrsCharacterStats> GetOsrsCharacterStats(string characterName);
    }
}
