namespace GoonHighScoresServer.Models
{
    public class CharacterLeaderboardEntry
    {
        public required Character Character { get; set; }
        public required List<XpDrop> XpDrops { get; set; }
    }
}
