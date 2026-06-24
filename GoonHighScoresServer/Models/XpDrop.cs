namespace GoonHighScoresServer.Models
{
    public class XpDrop
    {
        public int Id { get; set; }
        public int CharacterId { get; set; }
        public int SkillId { get; set; }
        public int Xp { get; set; }
        public int Level { get; set; }
        public int Rank { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
