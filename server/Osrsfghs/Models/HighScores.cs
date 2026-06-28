namespace Osrsfghs.Models
{
    public class HighScores
    {
        public required Character Character { get; set; }
        public int Xp { get; set; }
        public int Level { get; set; }
        public int Rank { get; set; }
    }
}
