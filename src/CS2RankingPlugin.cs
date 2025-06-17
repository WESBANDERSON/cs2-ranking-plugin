using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;

namespace CS2RankingPlugin
{
    public class CS2RankingPlugin : BasePlugin
    {
        public override string ModuleName => "CS2 Ranking Plugin";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "Your Name";
        public override string ModuleDescription => "A vanity-driven ranking system with in-game cosmetic rewards.";

        private const string ConfigPath = "addons/cs2-ranking/config.json";
        private string ConnectionString;
        private Dictionary<ulong, PlayerData> _playerData;
        private int _killXp = 10;
        private int _roundWinXp = 5;
        private int _levelUpXpMultiplier = 100;
        private Config _config;

        public override void Load(bool hotReload)
        {
            _config = LoadConfig();
            ConnectionString = $"Server={_config.DatabaseHost};Port={_config.DatabasePort};Database={_config.DatabaseName};User={_config.DatabaseUser};Password={_config.DatabasePassword};";
            _playerData = new Dictionary<ulong, PlayerData>();
            InitializeDatabase();

            CommandManager.AddCommand("rank", CommandRank, "Display player rank");
            CommandManager.AddCommand("top", CommandTop, "Display top players");
            CommandManager.AddCommand("prestige", CommandPrestige, "Prestige player");
            CommandManager.AddCommand("stats", CommandStats, "Display player statistics");
            CommandManager.AddCommand("css_lr_giveexp", CommandGiveExp, "Give XP to a player", CommandFlags.AdminOnly);
            CommandManager.AddCommand("css_lr_takeexp", CommandTakeExp, "Take XP from a player", CommandFlags.AdminOnly);
            CommandManager.AddCommand("css_lr_enabled", CommandToggleEnabled, "Toggle ranking system", CommandFlags.AdminOnly);

            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterEventHandler<EventRoundMvp>(OnRoundMvp);
            RegisterEventHandler<EventBombPlanted>(OnBombPlanted);
            RegisterEventHandler<EventBombDefused>(OnBombDefused);
            RegisterEventHandler<EventBombDropped>(OnBombDropped);
            RegisterEventHandler<EventBombPickup>(OnBombPickup);
        }

        private void InitializeDatabase()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new MySqlCommand(@"
                    CREATE TABLE IF NOT EXISTS players (
                        steam_id BIGINT PRIMARY KEY,
                        xp INT DEFAULT 0,
                        level INT DEFAULT 1,
                        prestige INT DEFAULT 0,
                        scoreboard_tag VARCHAR(50),
                        chat_color VARCHAR(20)
                    )", connection);
                command.ExecuteNonQuery();
            }
        }

        private void CommandRank(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;
            var steamId = player.SteamID;
            if (_playerData.TryGetValue(steamId, out var data))
                player.PrintToChat($"Your Rank: Level {data.Level} (Prestige {data.Prestige}) - XP: {data.XP}");
            else
                player.PrintToChat("No rank data found.");
        }

        private void CommandTop(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                var sql = "SELECT steam_id, level, prestige, xp FROM players ORDER BY xp DESC LIMIT 10";
                var cmd = new MySqlCommand(sql, connection);
                using (var reader = cmd.ExecuteReader())
                {
                    player.PrintToChat("Top Players:");
                    int rank = 1;
                    while (reader.Read())
                    {
                        var level = reader.GetInt32(1);
                        var prestige = reader.GetInt32(2);
                        var xp = reader.GetInt32(3);
                        player.PrintToChat($"{rank}. Level {level} (Prestige {prestige}) - XP: {xp}");
                        rank++;
                    }
                }
            }
        }

        private void CommandPrestige(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;
            var steamId = player.SteamID;
            if (_playerData.TryGetValue(steamId, out var data) && data.Level >= 55)
            {
                data.Prestige++;
                data.Level = 1;
                data.XP = 0;
                player.PrintToChat($"Prestiged to {data.Prestige}!");
                UpdatePlayerData(steamId, data);
            }
            else
            {
                player.PrintToChat("You must reach level 55 to prestige.");
            }
        }

        private void CommandStats(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;
            var steamId = player.SteamID;
            if (_playerData.TryGetValue(steamId, out var data))
            {
                player.PrintToChat($"Player Statistics for {player.PlayerName}:");
                player.PrintToChat($"Level: {data.Level}");
                player.PrintToChat($"Prestige: {data.Prestige}");
                player.PrintToChat($"XP: {data.XP}");
                player.PrintToChat($"Scoreboard Tag: {data.ScoreboardTag}");
                player.PrintToChat($"Chat Color: {data.ChatColor}");
            }
            else
            {
                player.PrintToChat("No statistics found.");
            }
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (Server.Players.Count < _config.MinPlayers) return HookResult.Continue;

            var attacker = @event.Attacker;
            var victim = @event.Userid;
            var assister = @event.Assister;

            if (attacker != null && attacker.IsValid)
            {
                var steamId = attacker.SteamID;
                if (_playerData.TryGetValue(steamId, out var data))
                {
                    // Check if it's a team kill
                    if (attacker.Team == victim.Team)
                    {
                        data.XP -= _config.XpSettings.EventPlayerDeath.KillingAnAlly;
                        attacker.PrintToChat($"{_config.Prefix} Team kill! -{_config.XpSettings.EventPlayerDeath.KillingAnAlly} XP");
                    }
                    else
                    {
                        data.XP += _config.XpSettings.EventPlayerDeath.Kills;
                        if (_config.ShowExperienceMessages)
                            attacker.PrintToChat($"{_config.Prefix} Kill! +{_config.XpSettings.EventPlayerDeath.Kills} XP");

                        // Check for weapon bonus
                        var weapon = attacker.ActiveWeapon?.DesignerName?.Replace("weapon_", "");
                        if (weapon != null && _config.XpSettings.Weapons.TryGetValue(weapon, out var bonus))
                        {
                            data.XP += bonus;
                            if (_config.ShowExperienceMessages)
                                attacker.PrintToChat($"{_config.Prefix} Weapon bonus! +{bonus} XP");
                        }
                    }
                    CheckLevelUp(attacker, data);
                    UpdatePlayerData(steamId, data);
                }
            }

            if (victim != null && victim.IsValid)
            {
                var steamId = victim.SteamID;
                if (_playerData.TryGetValue(steamId, out var data))
                {
                    data.XP -= _config.XpSettings.EventPlayerDeath.Deaths;
                    if (_config.ShowExperienceMessages)
                        victim.PrintToChat($"{_config.Prefix} Death! -{_config.XpSettings.EventPlayerDeath.Deaths} XP");
                    UpdatePlayerData(steamId, data);
                }
            }

            if (assister != null && assister.IsValid)
            {
                var steamId = assister.SteamID;
                if (_playerData.TryGetValue(steamId, out var data))
                {
                    data.XP += _config.XpSettings.EventPlayerDeath.Assists;
                    if (_config.ShowExperienceMessages)
                        assister.PrintToChat($"{_config.Prefix} Assist! +{_config.XpSettings.EventPlayerDeath.Assists} XP");
                    UpdatePlayerData(steamId, data);
                }
            }

            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            if (Server.Players.Count < _config.MinPlayers) return HookResult.Continue;

            var winner = @event.Winner;
            foreach (var player in Server.Players)
            {
                if (!player.IsValid) continue;
                var steamId = player.SteamID;
                if (!_playerData.TryGetValue(steamId, out var data)) continue;

                if (player.Team == winner)
                {
                    data.XP += _config.XpSettings.EventRoundEnd.Winner;
                    if (_config.ShowExperienceMessages)
                        player.PrintToChat($"{_config.Prefix} Round win! +{_config.XpSettings.EventRoundEnd.Winner} XP");
                }
                else
                {
                    data.XP -= _config.XpSettings.EventRoundEnd.Loser;
                    if (_config.ShowExperienceMessages)
                        player.PrintToChat($"{_config.Prefix} Round loss! -{_config.XpSettings.EventRoundEnd.Loser} XP");
                }
                CheckLevelUp(player, data);
                UpdatePlayerData(steamId, data);
            }
            return HookResult.Continue;
        }

        private HookResult OnRoundMvp(EventRoundMvp @event, GameEventInfo info)
        {
            if (Server.Players.Count < _config.MinPlayers) return HookResult.Continue;

            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                var steamId = player.SteamID;
                if (_playerData.TryGetValue(steamId, out var data))
                {
                    data.XP += _config.XpSettings.EventRoundMvp;
                    if (_config.ShowExperienceMessages)
                        player.PrintToChat($"{_config.Prefix} MVP! +{_config.XpSettings.EventRoundMvp} XP");
                    CheckLevelUp(player, data);
                    UpdatePlayerData(steamId, data);
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
        {
            if (Server.Players.Count < _config.MinPlayers) return HookResult.Continue;

            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                var steamId = player.SteamID;
                if (_playerData.TryGetValue(steamId, out var data))
                {
                    data.XP += _config.XpSettings.EventPlayerBomb.PlantedBomb;
                    if (_config.ShowExperienceMessages)
                        player.PrintToChat($"{_config.Prefix} Bomb planted! +{_config.XpSettings.EventPlayerBomb.PlantedBomb} XP");
                    CheckLevelUp(player, data);
                    UpdatePlayerData(steamId, data);
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
        {
            if (Server.Players.Count < _config.MinPlayers) return HookResult.Continue;

            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                var steamId = player.SteamID;
                if (_playerData.TryGetValue(steamId, out var data))
                {
                    data.XP += _config.XpSettings.EventPlayerBomb.DefusedBomb;
                    if (_config.ShowExperienceMessages)
                        player.PrintToChat($"{_config.Prefix} Bomb defused! +{_config.XpSettings.EventPlayerBomb.DefusedBomb} XP");
                    CheckLevelUp(player, data);
                    UpdatePlayerData(steamId, data);
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnBombDropped(EventBombDropped @event, GameEventInfo info)
        {
            if (Server.Players.Count < _config.MinPlayers) return HookResult.Continue;

            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                var steamId = player.SteamID;
                if (_playerData.TryGetValue(steamId, out var data))
                {
                    data.XP -= _config.XpSettings.EventPlayerBomb.DroppedBomb;
                    if (_config.ShowExperienceMessages)
                        player.PrintToChat($"{_config.Prefix} Bomb dropped! -{_config.XpSettings.EventPlayerBomb.DroppedBomb} XP");
                    UpdatePlayerData(steamId, data);
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnBombPickup(EventBombPickup @event, GameEventInfo info)
        {
            if (Server.Players.Count < _config.MinPlayers) return HookResult.Continue;

            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                var steamId = player.SteamID;
                if (_playerData.TryGetValue(steamId, out var data))
                {
                    data.XP += _config.XpSettings.EventPlayerBomb.PickUpBomb;
                    if (_config.ShowExperienceMessages)
                        player.PrintToChat($"{_config.Prefix} Bomb picked up! +{_config.XpSettings.EventPlayerBomb.PickUpBomb} XP");
                    CheckLevelUp(player, data);
                    UpdatePlayerData(steamId, data);
                }
            }
            return HookResult.Continue;
        }

        private void CommandGiveExp(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;
            if (command.ArgCount < 3)
            {
                player.PrintToChat($"{_config.Prefix} Usage: css_lr_giveexp <player> <amount>");
                return;
            }

            var targetName = command.GetArg(1);
            if (!int.TryParse(command.GetArg(2), out var amount))
            {
                player.PrintToChat($"{_config.Prefix} Invalid amount specified.");
                return;
            }

            var target = Server.Players.FirstOrDefault(p => p.PlayerName.Contains(targetName));
            if (target == null)
            {
                player.PrintToChat($"{_config.Prefix} Player not found.");
                return;
            }

            var steamId = target.SteamID;
            if (_playerData.TryGetValue(steamId, out var data))
            {
                data.XP += amount;
                player.PrintToChat($"{_config.Prefix} Gave {amount} XP to {target.PlayerName}");
                target.PrintToChat($"{_config.Prefix} Received {amount} XP from admin");
                CheckLevelUp(target, data);
                UpdatePlayerData(steamId, data);
            }
        }

        private void CommandTakeExp(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;
            if (command.ArgCount < 3)
            {
                player.PrintToChat($"{_config.Prefix} Usage: css_lr_takeexp <player> <amount>");
                return;
            }

            var targetName = command.GetArg(1);
            if (!int.TryParse(command.GetArg(2), out var amount))
            {
                player.PrintToChat($"{_config.Prefix} Invalid amount specified.");
                return;
            }

            var target = Server.Players.FirstOrDefault(p => p.PlayerName.Contains(targetName));
            if (target == null)
            {
                player.PrintToChat($"{_config.Prefix} Player not found.");
                return;
            }

            var steamId = target.SteamID;
            if (_playerData.TryGetValue(steamId, out var data))
            {
                data.XP = Math.Max(0, data.XP - amount);
                player.PrintToChat($"{_config.Prefix} Took {amount} XP from {target.PlayerName}");
                target.PrintToChat($"{_config.Prefix} Lost {amount} XP by admin");
                UpdatePlayerData(steamId, data);
            }
        }

        private void CommandToggleEnabled(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;
            if (command.ArgCount < 2)
            {
                player.PrintToChat($"{_config.Prefix} Usage: css_lr_enabled <0|1>");
                return;
            }

            if (!int.TryParse(command.GetArg(1), out var enabled) || (enabled != 0 && enabled != 1))
            {
                player.PrintToChat($"{_config.Prefix} Invalid value. Use 0 to disable or 1 to enable.");
                return;
            }

            _isEnabled = enabled == 1;
            player.PrintToChat($"{_config.Prefix} Ranking system {(enabled == 1 ? "enabled" : "disabled")}");
        }

        private void CheckLevelUp(CCSPlayerController player, PlayerData data)
        {
            if (data.XP >= data.Level * _levelUpXpMultiplier)
            {
                data.Level++;
                player.PrintToChat($"Level Up! You are now level {data.Level}.");
                UpdateCosmetics(player, data);
            }
        }

        private void UpdateCosmetics(CCSPlayerController player, PlayerData data)
        {
            data.ScoreboardTag = $"[P{data.Prestige} L{data.Level}]";
            data.ChatColor = data.Prestige switch
            {
                0 => "White",
                1 => "Green",
                2 => "Blue",
                3 => "Purple",
                4 => "Gold",
                5 => "Red",
                6 => "Orange",
                7 => "Pink",
                8 => "Cyan",
                9 => "Magenta",
                _ => "Rainbow"
            };
            player.PrintToChat($"Cosmetics updated: {data.ScoreboardTag} with {data.ChatColor} chat color.");
            ApplyPrestigeIcon(player, data.Prestige);
        }

        private void ApplyPrestigeIcon(CCSPlayerController player, int prestige)
        {
            player.PrintToChat($"Prestige Icon applied: Prestige {prestige} (PNG icon placeholder)");
        }

        private void UpdatePlayerData(ulong steamId, PlayerData data)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new MySqlCommand(@"
                    INSERT INTO players (steam_id, xp, level, prestige, scoreboard_tag, chat_color)
                    VALUES (@steamId, @xp, @level, @prestige, @scoreboardTag, @chatColor)
                    ON DUPLICATE KEY UPDATE
                    xp = @xp, level = @level, prestige = @prestige, scoreboard_tag = @scoreboardTag, chat_color = @chatColor", connection);
                command.Parameters.AddWithValue("@steamId", steamId);
                command.Parameters.AddWithValue("@xp", data.XP);
                command.Parameters.AddWithValue("@level", data.Level);
                command.Parameters.AddWithValue("@prestige", data.Prestige);
                command.Parameters.AddWithValue("@scoreboardTag", data.ScoreboardTag);
                command.Parameters.AddWithValue("@chatColor", data.ChatColor);
                command.ExecuteNonQuery();
            }
        }

        private void LoadPlayerData(CCSPlayerController player)
        {
            var steamId = player.SteamID;
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new MySqlCommand("SELECT xp, level, prestige, scoreboard_tag, chat_color FROM players WHERE steam_id = @steamId", connection);
                command.Parameters.AddWithValue("@steamId", steamId);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var data = new PlayerData
                        {
                            XP = reader.GetInt32(0),
                            Level = reader.GetInt32(1),
                            Prestige = reader.GetInt32(2),
                            ScoreboardTag = reader.GetString(3),
                            ChatColor = reader.GetString(4)
                        };
                        _playerData[steamId] = data;
                        player.PrintToChat($"Welcome back! Level {data.Level} (Prestige {data.Prestige})");
                    }
                    else
                    {
                        _playerData[steamId] = new PlayerData();
                        player.PrintToChat("Welcome! You are now level 1.");
                    }
                }
            }
        }

        private void SavePlayerData(CCSPlayerController player)
        {
            var steamId = player.SteamID;
            if (_playerData.TryGetValue(steamId, out var data))
            {
                UpdatePlayerData(steamId, data);
                _playerData.Remove(steamId);
            }
        }

        private Config LoadConfig()
        {
            var configPath = ConfigPath;
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));
            if (!File.Exists(configPath))
            {
                var defaultConfig = new Config
                {
                    DatabaseHost = "localhost",
                    DatabasePort = 3306,
                    DatabaseName = "rkb_vanity",
                    DatabaseUser = "rkb",
                    DatabasePassword = "dD7xPpfyfd"
                };
                File.WriteAllText(configPath, JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true }));
                return defaultConfig;
            }
            return JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));
        }

        private class Config
        {
            public string DatabaseHost { get; set; }
            public int DatabasePort { get; set; }
            public string DatabaseName { get; set; }
            public string DatabaseUser { get; set; }
            public string DatabasePassword { get; set; }
            public string Prefix { get; set; } = "[ {BLUE}Ranks {DEFAULT}]";
            public bool UseCommandWithoutPrefix { get; set; } = true;
            public bool ShowExperienceMessages { get; set; } = true;
            public int MinPlayers { get; set; } = 4;
            public int InitialExperiencePoints { get; set; } = 1000;
            public XpSettings XpSettings { get; set; } = new XpSettings();
        }

        private class XpSettings
        {
            public int EventRoundMvp { get; set; } = 12;
            public PlayerDeathXp EventPlayerDeath { get; set; } = new PlayerDeathXp();
            public PlayerBombXp EventPlayerBomb { get; set; } = new PlayerBombXp();
            public RoundEndXp EventRoundEnd { get; set; } = new RoundEndXp();
            public Dictionary<string, int> Weapons { get; set; } = new Dictionary<string, int>
            {
                { "knife", 5 },
                { "awp", 2 }
            };
        }

        private class PlayerDeathXp
        {
            public int Kills { get; set; } = 13;
            public int Deaths { get; set; } = 20;
            public int Assists { get; set; } = 5;
            public int KillingAnAlly { get; set; } = 6;
        }

        private class PlayerBombXp
        {
            public int DroppedBomb { get; set; } = 5;
            public int PlantedBomb { get; set; } = 3;
            public int DefusedBomb { get; set; } = 3;
            public int PickUpBomb { get; set; } = 3;
        }

        private class RoundEndXp
        {
            public int Winner { get; set; } = 5;
            public int Loser { get; set; } = 8;
        }
    }

    public class PlayerData
    {
        public int XP { get; set; }
        public int Level { get; set; }
        public int Prestige { get; set; }
        public string ScoreboardTag { get; set; }
        public string ChatColor { get; set; }
        public PlayerData()
        {
            XP = 0;
            Level = 1;
            Prestige = 0;
            ScoreboardTag = "";
            ChatColor = "";
        }
    }
} 