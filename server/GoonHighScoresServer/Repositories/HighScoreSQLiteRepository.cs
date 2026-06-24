using System.Data;
using System.Data.SQLite;
using GoonHighScoresServer.Interfaces;
using GoonHighScoresServer.Models;
using Microsoft.Extensions.Options;

namespace GoonHighScoresServer.Repositories
{
    public class HighScoreSQLiteRepository : IHighScoreRepository
    {
        private readonly HighScoreSQLiteRepositoryOptions _options;

        public HighScoreSQLiteRepository(IOptions<HighScoreSQLiteRepositoryOptions> options)
        {
            _options = options.Value;
        }

        public async Task<int> GetCharacterId(string characterName)
        {
            int characterId = -1;

            using(SQLiteConnection connection = new SQLiteConnection(_options.ConnectionString))
            {
                await connection.OpenAsync();
                using(SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT Id from Character where Name = @name";
                    command.Parameters.AddWithValue("@name", characterName);
                    command.Prepare();

                    using(SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            int id = reader.GetInt32(reader.GetOrdinal("Id"));

                            characterId = id;
                        }
                    }
                }

                await connection.CloseAsync();
            }

            return characterId;
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
                        Select XpDrop.Xp, XpDrop.SkillId, XpDrop.Timestamp, Character.Name, Character.Id as CharacterId from XpDrop INNER JOIN Character ON XpDrop.CharacterId = Character.Id
                        WHERE XpDrop.Timestamp > @cutoffDate AND XpDrop.SkillId = 0

                        UNION ALL

                        Select XpDrop.Xp, XpDrop.SkillId, XpDrop.Timestamp, Character.Name, Character.Id as CharacterId from XpDrop INNER JOIN Character ON XpDrop.CharacterId = Character.Id
                        WHERE XpDrop.Timestamp = (
                        SELECT MAX(XpDrop.Timestamp)
                        FROM XpDrop
                        WHERE XpDrop.Timestamp <= @cutoffDate) AND XpDrop.SkillId = 0; 
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
                                        Id = characterId
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
                            //string discordUserId = reader.GetString(reader.GetOrdinal("DiscordUserId"));

                            Character character = new Character()
                            {
                                Id = id,
                                Name = characterName,
                                //    DiscordUserId = discordUserId,
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

    public class HighScoreSQLiteRepositoryOptions
        {
            public required string ConnectionString { get; set; }
        }
    }
}
