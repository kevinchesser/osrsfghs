namespace GoonHighScoresServer.Models
{
    public class Character
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        //TODO: Discord user avatar api
        //public string? DiscordUserId { get; set; }
    }
}
