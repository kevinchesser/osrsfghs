using GoonHighScoresServer.Interfaces;
using GoonHighScoresServer.Models;

namespace GoonHighScoresServer.Services
{
    public class TrackedCharacterStore : ITrackedCharacterStore
    {
        private readonly ILogger<TrackedCharacterStore> _logger;
        private readonly object _lock = new object();
        private List<Character> _trackedCharacters = new List<Character>();
        
        public TrackedCharacterStore(ILogger<TrackedCharacterStore> logger)
        {
            _logger = logger;
        }

        public IReadOnlyList<Character> GetTrackedCharacters()
        {
            lock(_lock)
            {
                return _trackedCharacters.AsReadOnly();
            }
        }

        public void SetTrackedCharacters(List<Character> characters)
        {
            lock(_lock)
            {
                if(characters != null && characters.Count != 0)
                    _trackedCharacters = characters;
                else
                    _logger.LogWarning("Attemping to set trackedCharacter list to null or empty list");
            }
        }
    }
}
