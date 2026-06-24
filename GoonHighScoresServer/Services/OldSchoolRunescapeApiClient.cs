using GoonHighScoresServer.Interfaces;
using GoonHighScoresServer.Models;

namespace GoonHighScoresServer.Services
{
    public class OldSchoolRunescapeApiClient : IOldSchoolRunescapeApiClient
    {
        private readonly ILogger<OldSchoolRunescapeApiClient> _logger;
        private readonly HttpClient _oldSchoolRunescapeApiHttpClient;

        public OldSchoolRunescapeApiClient(ILogger<OldSchoolRunescapeApiClient> logger, HttpClient httpclient)
        {
            _logger = logger;
            _oldSchoolRunescapeApiHttpClient = httpclient;
        }

        public async Task<OsrsCharacterStats> GetOsrsCharacterStats(string characterName)
        {
            try
            {
                HttpResponseMessage response = await _oldSchoolRunescapeApiHttpClient.GetAsync($"m=hiscore_oldschool/index_lite.json?player={characterName}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<OsrsCharacterStats>();
            }
            catch(HttpRequestException exception)
            {
                _logger.LogError(exception, "Unable to get stats for {characterName}", characterName);
                throw;
            }
        }
    }
}
