using System.Data;
using System.Data.SQLite;
using Osrsfghs.Interfaces;
using Osrsfghs.Models;
using Microsoft.Extensions.Options;

namespace Osrsfghs.Repositories
{
    public class HighScoreSQLiteRepository : IHighScoreRepository
    {
        private readonly HighScoreSQLiteRepositoryOptions _options;

        public HighScoreSQLiteRepository(IOptions<HighScoreSQLiteRepositoryOptions> options)
        {
            _options = options.Value;
        }

        public async Task<Character> GetCharacter(string characterName)
        {
            Character character = new Character() { Name = characterName };

            using(SQLiteConnection connection = new SQLiteConnection(_options.ConnectionString))
            {
                await connection.OpenAsync();
                using(SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT Id, AvatarUrl from Character where Name = @name";
                    command.Parameters.AddWithValue("@name", characterName);
                    command.Prepare();

                    using(SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            int id = reader.GetInt32(reader.GetOrdinal("Id"));
                            int avatarUrlOrdinal = reader.GetOrdinal("AvatarUrl");
                            string? avatarUrl = null;
                            if(!reader.IsDBNull(avatarUrlOrdinal))
                                avatarUrl = reader.GetString(reader.GetOrdinal("AvatarUrl"));

                            character.Id = id;
                            character.AvatarUrl = avatarUrl;
                        }
                    }
                }

                await connection.CloseAsync();
            }

            return character;
        }

        public async Task<List<XpDrop>> GetAllXpDropsAndFallbackIfNoXpDropWithinCutoff(int characterId, DateTime backdatedDateTimeUtc)
        {
            List<XpDrop> xpDrops = new List<XpDrop>();

            using(SQLiteConnection connection = new SQLiteConnection(_options.ConnectionString))
            {
                await connection.OpenAsync();
                using(SQLiteCommand command = new SQLiteCommand(connection))
                {
                    string sqlCommand = @"
                       SELECT Xp, SkillId, Timestamp, Level, Rank
                       FROM XpDrop AS x
                       WHERE x.CharacterId = @characterId
                         AND x.TimeStamp >= @cutoffDate

                       UNION ALL

                       SELECT xd.Xp, xd.SkillId, xd.Timestamp, xd.Level, xd.Rank
                       FROM XpDrop AS xd
                       WHERE xd.CharacterId = @characterId
                         AND xd.Id = (
                             SELECT x2.Id
                             FROM XpDrop x2
                             WHERE x2.CharacterId = xd.CharacterId
                               AND x2.SkillId = xd.SkillId
                             ORDER BY x2.TimeStamp DESC
                             LIMIT 1
                         )
                         AND NOT EXISTS (
                             SELECT 1
                             FROM XpDrop recent
                             WHERE recent.CharacterId = xd.CharacterId
                               AND recent.SkillId = xd.SkillId
                               AND recent.TimeStamp >= @cutoffDate
                         )
                       ORDER BY SkillId, TimeStamp;
                       ";
                    command.CommandText = sqlCommand;
                    command.Parameters.AddWithValue("@characterId", characterId);
                    command.Parameters.AddWithValue("@cutoffDate", backdatedDateTimeUtc);
                    command.Prepare();

                    using(SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            int xp = reader.GetInt32(reader.GetOrdinal("Xp"));
                            int skillId = reader.GetInt32(reader.GetOrdinal("SkillId"));
                            DateTime timeStamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("Timestamp")));
                            int level = reader.GetInt32(reader.GetOrdinal("Level"));
                            int rank = reader.GetInt32(reader.GetOrdinal("Rank"));

                            XpDrop xpDrop = new XpDrop()
                            {
                                Xp = xp,
                                SkillId = skillId,
                                TimeStamp = DateTime.SpecifyKind(timeStamp, DateTimeKind.Utc),
                                Level = level,
                                Rank = rank
                            };
                            xpDrops.Add(xpDrop);
                        }
                    }
                }
            }

            return xpDrops;
        }

        public async Task<Dictionary<int, CharacterLeaderboardEntry>> GetCharacterLeaderboardEntriesForOverallXp(DateTime backdatedDateTimeUtc)
        {
            Dictionary<int, CharacterLeaderboardEntry> overallXpDropsDictionary = new Dictionary<int, CharacterLeaderboardEntry>();

            using(SQLiteConnection connection = new SQLiteConnection(_options.ConnectionString))
            {
                await connection.OpenAsync();
                using(SQLiteCommand command = new SQLiteCommand(connection))
                {
                    string sqlCommand = @"
                        WITH RankedPrior AS (
                            SELECT XpDrop.Xp, XpDrop.SkillId, XpDrop.Timestamp, Character.Name, Character.Id AS CharacterId, Character.AvatarUrl,
                                   ROW_NUMBER() OVER (
                                       PARTITION BY XpDrop.CharacterId
                                       ORDER BY XpDrop.Timestamp DESC
                                   ) AS rn
                            FROM XpDrop
                            INNER JOIN Character ON XpDrop.CharacterId = Character.Id
                            WHERE XpDrop.SkillId = 0
                              AND XpDrop.Timestamp <= @cutoffDate
                        )
                        SELECT Xp, SkillId, Timestamp, Name, CharacterId, AvatarUrl
                        FROM XpDrop
                        INNER JOIN Character ON XpDrop.CharacterId = Character.Id
                        WHERE XpDrop.SkillId = 0
                          AND XpDrop.Timestamp > @cutoffDate

                        UNION ALL

                        SELECT Xp, SkillId, Timestamp, Name, CharacterId, AvatarUrl
                        FROM RankedPrior
                        WHERE rn = 1;
                        ";
                    command.CommandText = sqlCommand;
                    command.Parameters.AddWithValue("@cutoffDate", backdatedDateTimeUtc);
                    command.Prepare();

                    using(SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            int xp = reader.GetInt32(reader.GetOrdinal("Xp"));
                            int skillId = reader.GetInt32(reader.GetOrdinal("SkillId"));
                            DateTime timeStamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("Timestamp")));
                            string characterName = reader.GetString(reader.GetOrdinal("Name"));
                            int characterId = reader.GetInt32(reader.GetOrdinal("CharacterId"));

                            int avatarUrlOrdinal = reader.GetOrdinal("AvatarUrl");
                            string? avatarUrl = null;
                            if(!reader.IsDBNull(avatarUrlOrdinal))
                                avatarUrl = reader.GetString(reader.GetOrdinal("AvatarUrl"));

                            if(!overallXpDropsDictionary.ContainsKey(characterId))
                            {
                                XpDrop xpDrop = new XpDrop()
                                {
                                    Xp = xp,
                                    SkillId = skillId,
                                    TimeStamp = timeStamp
                                };
                                overallXpDropsDictionary.Add(characterId, new CharacterLeaderboardEntry()
                                {
                                    Character = new Character()
                                    {
                                        Name = characterName,
                                        Id = characterId,
                                        AvatarUrl = avatarUrl
                                    },
                                    XpDrops = new List<XpDrop>() { xpDrop }
                                });
                            }
                            else
                            {
                                XpDrop xpDrop = new XpDrop()
                                {
                                    Xp = xp,
                                    SkillId = skillId,
                                    TimeStamp = timeStamp
                                };

                                overallXpDropsDictionary[characterId].XpDrops.Add(xpDrop);
                            }
                        }
                    }
                }

                await connection.CloseAsync();
            }

            return overallXpDropsDictionary;
        }

        public async Task<List<Character>> GetCharacters()
        {
            List<Character> characters = new List<Character>(10);

            using(SQLiteConnection connection = new SQLiteConnection(_options.ConnectionString))
            {
                await connection.OpenAsync();
                using(SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT * from Character";
                    command.Prepare();

                    using(SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            int id = reader.GetInt32(reader.GetOrdinal("Id"));
                            string characterName = reader.GetString(reader.GetOrdinal("Name"));
                            int discordUserIdOrdinal = reader.GetOrdinal("DiscordUserId");
                            string? discordUserId = null;
                            if (!reader.IsDBNull(discordUserIdOrdinal))
                                discordUserId = reader.GetString(reader.GetOrdinal("DiscordUserId"));

                            int avatarUrlOrdinal = reader.GetOrdinal("AvatarUrl");
                            string? avatarUrl = null;
                            if (!reader.IsDBNull(avatarUrlOrdinal))
                                avatarUrl = reader.GetString(reader.GetOrdinal("AvatarUrl"));

                            Character character = new Character()
                            {
                                Id = id,
                                Name = characterName,
                                DiscordUserId = discordUserId,
                                AvatarUrl = avatarUrl
                            };
                            characters.Add(character);
                        }
                    }
                }

                await connection.CloseAsync();
            }

            return characters;
        }

        public async Task<int?> GetMostRecentOverallXp(int characterId)
        {
            int? overallXp = null;

            using(SQLiteConnection connection = new SQLiteConnection(_options.ConnectionString))
            {
                await connection.OpenAsync();
                using(SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT MAX(Xp) as MaxXp from XpDrop WHERE CharacterId = @characterId AND SkillId = @skillId";
                    command.Parameters.AddWithValue("@characterId", characterId);
                    command.Parameters.AddWithValue("@skillId", (int)Enums.OsrsSkill.Overall);
                    command.Prepare();

                    object? result = await command.ExecuteScalarAsync();
                    if(result != DBNull.Value)
                        overallXp = Convert.ToInt32(result);
                }

                await connection.CloseAsync();
            }

            return overallXp;
        }

        public async Task<Dictionary<int, int>> GetMostRecentXpDrops(int characterId)
        {
            Dictionary<int, int> mostRecentXpDrops = new Dictionary<int, int>();

            using(SQLiteConnection connection = new SQLiteConnection(_options.ConnectionString))
            {
                await connection.OpenAsync();
                using(SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT MAX(Xp) as MaxXp, SkillId from XpDrop WHERE CharacterId = @characterId Group BY SkillId";
                    command.Parameters.AddWithValue("@characterId", characterId);
                    command.Prepare();

                    using(SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            int xp = reader.GetInt32(reader.GetOrdinal("MaxXp"));
                            int skillId = reader.GetInt32(reader.GetOrdinal("SkillId"));

                            mostRecentXpDrops.Add(skillId, xp);
                        }
                    }
                }

                await connection.CloseAsync();
            }

            return mostRecentXpDrops;
        }

        public async Task SaveXpDrops(List<XpDrop> xpDrops, string processingTime)
        {
            using(SQLiteConnection connection = new SQLiteConnection(_options.ConnectionString))
            {
                await connection.OpenAsync();
                using SQLiteTransaction transaction = connection.BeginTransaction();
                using(SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "INSERT INTO XpDrop (CharacterId, SkillId, Xp, Level, Rank, Timestamp) VALUES (@characterId, @skillId, @xp, @level, @rank, @timeStamp)";
                    SQLiteParameter characterIdParameter = command.Parameters.Add("@characterId", DbType.Int32);
                    SQLiteParameter skillIdParameter = command.Parameters.Add("@skillId", DbType.Int32);
                    SQLiteParameter xpParameter = command.Parameters.Add("@xp", DbType.Int32);
                    SQLiteParameter levelParameter = command.Parameters.Add("@level", DbType.Int32);
                    SQLiteParameter rankParameter = command.Parameters.Add("@rank", DbType.Int32);
                    command.Parameters.AddWithValue("@timeStamp", processingTime);

                    foreach(XpDrop xpDrop in xpDrops)
                    {
                        characterIdParameter.Value = xpDrop.CharacterId;
                        skillIdParameter.Value = xpDrop.SkillId;
                        xpParameter.Value = xpDrop.Xp;
                        levelParameter.Value = xpDrop.Level;
                        rankParameter.Value = xpDrop.Rank;

                        command.ExecuteNonQuery();
                    }
                }
                transaction.Commit();

                await connection.CloseAsync();
            }
        }

        public async Task UpdateAvatarUrlAsync(int characterId, string avatarUrl)
        {
            using(SQLiteConnection connection = new SQLiteConnection(_options.ConnectionString))
            {
                await connection.OpenAsync();
                using(SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "UPDATE Character SET AvatarUrl = @avatarUrl WHERE Id = @characterId";
                    command.Parameters.AddWithValue("@avatarUrl", avatarUrl);
                    command.Parameters.AddWithValue("@characterId", characterId);

                    await command.ExecuteNonQueryAsync();
                }

                await connection.CloseAsync();
            }
        }

        public async Task<Dictionary<int, List<HighScores>>> GetHighScoresForAllSkills()
        {
            Dictionary<int, List<HighScores>> highScoresBySkill = new Dictionary<int, List<HighScores>>();

            using(SQLiteConnection connection = new SQLiteConnection(_options.ConnectionString))
            {
                await connection.OpenAsync();
                using(SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                        SELECT XpDrop.SkillId, XpDrop.Xp, XpDrop.Level, XpDrop.Rank,
                               Character.Id AS CharacterId, Character.Name, Character.DiscordUserId, Character.AvatarUrl
                        FROM XpDrop
                        INNER JOIN Character ON XpDrop.CharacterId = Character.Id
                        INNER JOIN (
                            SELECT CharacterId, SkillId, MAX(Timestamp) AS MaxTimestamp
                            FROM XpDrop
                            GROUP BY CharacterId, SkillId
                        ) AS Latest
                        ON XpDrop.CharacterId = Latest.CharacterId
                        AND XpDrop.SkillId = Latest.SkillId
                        AND XpDrop.Timestamp = Latest.MaxTimestamp
                        ORDER BY XpDrop.SkillId, XpDrop.Xp DESC";
                    command.Prepare();

                    using(SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            int skillId = reader.GetInt32(reader.GetOrdinal("SkillId"));

                            HighScores highScore = new HighScores()
                            {
                                Character = new Character()
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("CharacterId")),
                                    Name = reader.GetString(reader.GetOrdinal("Name")),
                                    DiscordUserId = reader.IsDBNull(reader.GetOrdinal("DiscordUserId"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("DiscordUserId")),
                                    AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("AvatarUrl"))
                                },
                                Xp = reader.GetInt32(reader.GetOrdinal("Xp")),
                                Level = reader.GetInt32(reader.GetOrdinal("Level")),
                                Rank = reader.GetInt32(reader.GetOrdinal("Rank"))
                            };

                            if(!highScoresBySkill.TryGetValue(skillId, out List<HighScores>? characterHighScores))
                            {
                                characterHighScores = new List<HighScores>();
                                highScoresBySkill[skillId] = characterHighScores;
                            }

                            characterHighScores.Add(highScore);
                        }
                    }
                }

                await connection.CloseAsync();
            }

            return highScoresBySkill;
        }

        public class HighScoreSQLiteRepositoryOptions
        {
            public required string ConnectionString { get; set; }
        }
    }
}
