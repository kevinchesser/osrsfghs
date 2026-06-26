using Osrsfghs.Models;

namespace Osrsfghs.Interfaces
{
    public interface ITrackedCharacterStore
    {
        IReadOnlyList<Character> GetTrackedCharacters();
        void SetTrackedCharacters(List<Character> characters);
    }
}
