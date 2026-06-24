using GoonHighScoresServer.Exceptions;
using GoonHighScoresServer.Interfaces;
using GoonHighScoresServer.Models;

namespace GoonHighScoresServer.Services
{
    public class HighScoreService : IHighScoreService
    {
        private readonly ILogger<HighScoreService> _logger;
        private readonly IHighScoreRepository _highScoreRepository;
        private static readonly TimeSpan OneWeekTimeSpan = TimeSpan.FromDays(7);

        public HighScoreService(ILogger<HighScoreService> logger, IHighScoreRepository highScoreRepository)
        {
            _logger = logger;
            _highScoreRepository = highScoreRepository;
        }

        public async Task<List<Character>> GetCharacters()
        {
            return await _highScoreRepository.GetCharacters();
        }

        public async Task<CharacterOverview> GetCharacterOverview(string characterName)
        {
            CharacterOverview characterOverview = new CharacterOverview();
            int characterId = await _highScoreRepository.GetCharacterId(characterName);
            if(characterId == -1)
                throw new CharacterNotFoundException($"{characterName} is not being tracked");

            List<XpDrop> allXpDropsForCharacter = await _highScoreRepository.GetAllXpDropsAndFallbackIfNoXpDropWithinCutoff(characterId, DateTime.UtcNow - OneWeekTimeSpan);

            characterOverview.Character = new Character()
            { 
                Name = characterName,
                Id = characterId
            };
            characterOverview.XpDropsBySkill = allXpDropsForCharacter.GroupBy(x => x.SkillId).ToDictionary(x => x.Key, y => y.OrderByDescending(x => x.TimeStamp).ToList());

            return characterOverview;
        }

        public async Task<TimespanXpLeaderboardViewModel> GetLastXTimeSpanOverallXpLeadboard(TimeSpan lookbackTimeSpan)
        {
            TimespanXpLeaderboardViewModel timespanXpLeaderboardViewModel = new TimespanXpLeaderboardViewModel()
            {
                TimeSpanUsed = lookbackTimeSpan
            };

            Dictionary<int, CharacterLeaderboardEntry> characterLeaderboardEntryDict =  await _highScoreRepository.GetCharacterLeaderboardEntriesForOverallXp(DateTime.UtcNow - lookbackTimeSpan);
            List<TimeSpanLeaderboardItem> timeSpanLeaderboardItems = new List<TimeSpanLeaderboardItem>();
            timespanXpLeaderboardViewModel.TimeSpanLeaderboardItems = timeSpanLeaderboardItems;
            foreach (CharacterLeaderboardEntry characterLeaderboardEntry in characterLeaderboardEntryDict.Values)
            {
                if(characterLeaderboardEntry.XpDrops.Count < 2)
                    continue;

                int earliestXp = characterLeaderboardEntry.XpDrops.MinBy(x => x.TimeStamp)!.Xp;
                int latestXp = characterLeaderboardEntry.XpDrops.MaxBy(x => x.TimeStamp)!.Xp;
                int xpGained = latestXp - earliestXp;

                timeSpanLeaderboardItems.Add(new TimeSpanLeaderboardItem() 
                { 
                    Character = characterLeaderboardEntry.Character, 
                    TimeSpanOverallXp = xpGained
                });
            }
            timespanXpLeaderboardViewModel.TimeSpanLeaderboardItems = timespanXpLeaderboardViewModel.TimeSpanLeaderboardItems.OrderByDescending(x => x.TimeSpanOverallXp).ToList();

            return timespanXpLeaderboardViewModel;
        }

        public async Task RecordXpDropsIfNecessary(Character character, Dictionary<int, OsrsSkill> osrsCharacterStats, string processingTime)
        {
            List<XpDrop> xpDropsToRecord = new List<XpDrop>(24);
            int? mostRecentOverallXp = await _highScoreRepository.GetMostRecentOverallXp(character.Id);
            if(!mostRecentOverallXp.HasValue)
            {
                foreach(OsrsSkill osrsSkill in osrsCharacterStats.Values)
                {
                    XpDrop xpDrop = new XpDrop()
                    {
                        CharacterId = character.Id,
                        SkillId = osrsSkill.Id,
                        Xp = osrsSkill.Xp,
                        Level = osrsSkill.Level,
                        Rank = osrsSkill.Rank
                    };
                    xpDropsToRecord.Add(xpDrop);
                }
            }
            else
            {
                Dictionary<int, int> mostRecentXpDrops = await _highScoreRepository.GetMostRecentXpDrops(character.Id);
                foreach(OsrsSkill osrsSkill in osrsCharacterStats.Values)
                {
                    if (!mostRecentXpDrops.ContainsKey(osrsSkill.Id) || (mostRecentXpDrops.ContainsKey(osrsSkill.Id) && mostRecentXpDrops[osrsSkill.Id] < osrsSkill.Xp))
                    {
                        XpDrop xpDrop = new XpDrop()
                        {
                            CharacterId = character.Id,
                            SkillId = osrsSkill.Id,
                            Xp = osrsSkill.Xp,
                            Level = osrsSkill.Level,
                            Rank = osrsSkill.Rank
                        };
                        xpDropsToRecord.Add(xpDrop);
                    }
                }
            }

            if (xpDropsToRecord.Count > 0)
            {
                _logger.LogInformation("Record {numberOf} XpDrops for {CharacterId}", xpDropsToRecord.Count, character.Id);
                await _highScoreRepository.SaveXpDrops(xpDropsToRecord, processingTime);
            }
        }
    }
}
