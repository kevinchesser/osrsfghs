namespace GoonHighScoresServer.Models
{
    public class TimespanXpLeaderboardViewModel
    {
        public List<TimeSpanLeaderboardItem>? TimeSpanLeaderboardItems { get; set; }
        public TimeSpan TimeSpanUsed { get; set; }
    }
}
