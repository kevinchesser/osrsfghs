using GoonHighScoresServer.Models;

namespace GoonHighScoresServer.Interfaces
{
    public interface ITrackedCharacterStore
    {
        IReadOnlyList<Character> GetTrackedCharacters();
        void SetTrackedCharacters(List<Character> characters);
    }
}
