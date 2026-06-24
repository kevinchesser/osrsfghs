namespace GoonHighScoresServer.Models
{
    public class CharacterOverview
    {
        public Character Character { get; set; }
        public Dictionary<int, List<XpDrop>> XpDropsBySkill { get; set; }
    }
}
