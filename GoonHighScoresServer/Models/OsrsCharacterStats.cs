namespace GoonHighScoresServer.Models
{
    public class OsrsCharacterStats
    {
        public required string Name { get; set; }
        public required List<OsrsSkill> Skills { get; set; }
    }
}
