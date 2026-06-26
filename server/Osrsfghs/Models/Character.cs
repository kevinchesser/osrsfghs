namespace Osrsfghs.Models
{
    public class Character
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? DiscordUserId { get; set; }
    }
}
