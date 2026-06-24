namespace GoonHighScoresServer.Models
{
    public class OsrsSkill
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required int Rank { get; set; } 
        public required int Level { get; set; }
        public required int Xp { get; set; }
    }
}
