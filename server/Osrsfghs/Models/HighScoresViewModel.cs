namespace Osrsfghs.Models
{
    public class HighScoresViewModel
    {
        public required Dictionary<int, List<HighScores>> HighScoresBySkill { get; set; }
    }
}
