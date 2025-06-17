using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CS2RankingPlugin
{
    [MinimumApiVersion(1, 0, 0)]
    public class CS2RankingPlugin : BasePlugin
    {
        public override string ModuleName => "CS2 Ranking Plugin";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "Your Name";
        public override string ModuleDescription => "A vanity-driven ranking system with in-game cosmetic rewards.";

        private string _connectionString;
        private Dictionary<ulong, PlayerData> _playerData;
        private int _killXp = 10;
        private int _roundWinXp = 5;
        private int _levelUpXpMultiplier = 100;

        public override void Load(bool hotReload)
        {
            _connectionString = "Server=rkb.site.nfoservers.com;Port=3306;Database=cs2_ranking;User=rkb;Password=dD7xPpfyfd;";
            _playerData = new Dictionary<ulong, PlayerData>();

            // Register commands
            RegisterCommand("rank", "Display player rank", CommandRank);
            RegisterCommand("top", "Display top players", CommandTop);
            RegisterCommand("prestige", "Prestige player", CommandPrestige);
            RegisterCommand("config", "Configure plugin settings", CommandConfig);
            RegisterCommand("givexp", "Give XP to a player (Admin only)", CommandGiveXp);
            RegisterCommand("takexp", "Take XP from a player (Admin only)", CommandTakeXp);
            RegisterCommand("levelup", "Level up a player (Admin only)", CommandLevelUp);
            RegisterCommand("stats", "Display player statistics", CommandStats);
            RegisterCommand("reset", "Reset player data (Admin only)", CommandReset);
            RegisterCommand("help", "Display help information", CommandHelp);
            RegisterCommand("achievements", "Display player achievements", CommandAchievements);
            RegisterCommand("leaderboard", "Display player leaderboard", CommandLeaderboard);
            RegisterCommand("progress", "Display player progress", CommandProgress);
            RegisterCommand("rewards", "Display player rewards", CommandRewards);
            RegisterCommand("settings", "Display player settings", CommandSettings);
            RegisterCommand("version", "Display plugin version", CommandVersion);
            RegisterCommand("info", "Display player info", CommandInfo);
            RegisterCommand("status", "Display player status", CommandStatus);
            RegisterCommand("profile", "Display player profile", CommandProfile);
            RegisterCommand("history", "Display player history", CommandHistory);

            // Hook events
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

            // Initialize database
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new MySqlConnection(_connectionString))
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
            {
                player.PrintToChat($"Your Rank: Level {data.Level} (Prestige {data.Prestige}) - XP: {data.XP}");
            }
            else
            {
                player.PrintToChat("No rank data found.");
            }
        }

        private void CommandTop(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var command = new MySqlCommand(@"
                    SELECT steam_id, level, prestige, xp FROM players ORDER BY xp DESC LIMIT 10", connection);
                using (var reader = command.ExecuteReader())
                {
                    player.PrintToChat("Top Players:");
                    int rank = 1;
                    while (reader.Read())
                    {
                        var steamId = reader.GetInt64(0);
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
                // Update database
                UpdatePlayerData(steamId, data);
            }
            else
            {
                player.PrintToChat("You must reach level 55 to prestige.");
            }
        }

        private void CommandConfig(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsAdmin) return;

            if (command.ArgCount < 3)
            {
                player.PrintToChat("Usage: !config <setting> <value>");
                player.PrintToChat("Settings: killXp, roundWinXp, levelUpXpMultiplier");
                return;
            }

            var setting = command.GetArg(1);
            var value = command.GetArg(2);

            if (int.TryParse(value, out int intValue))
            {
                switch (setting.ToLower())
                {
                    case "killxp":
                        _killXp = intValue;
                        player.PrintToChat($"Kill XP set to {_killXp}");
                        break;
                    case "roundwinxp":
                        _roundWinXp = intValue;
                        player.PrintToChat($"Round Win XP set to {_roundWinXp}");
                        break;
                    case "levelupxpmultiplier":
                        _levelUpXpMultiplier = intValue;
                        player.PrintToChat($"Level Up XP Multiplier set to {_levelUpXpMultiplier}");
                        break;
                    default:
                        player.PrintToChat("Invalid setting.");
                        break;
                }
            }
            else
            {
                player.PrintToChat("Invalid value. Please provide a number.");
            }
        }

        private void CommandGiveXp(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsAdmin) return;

            if (command.ArgCount < 3)
            {
                player.PrintToChat("Usage: !givexp <player> <amount>");
                return;
            }

            var targetPlayer = command.GetArg(1);
            var amount = command.GetArg(2);

            if (int.TryParse(amount, out int xpAmount))
            {
                var target = Utilities.GetPlayerFromName(targetPlayer);
                if (target != null)
                {
                    var steamId = target.SteamID;
                    if (_playerData.TryGetValue(steamId, out var data))
                    {
                        data.XP += xpAmount;
                        player.PrintToChat($"Gave {xpAmount} XP to {targetPlayer}.");
                        CheckLevelUp(target, data);
                        UpdatePlayerData(steamId, data);
                    }
                }
                else
                {
                    player.PrintToChat("Player not found.");
                }
            }
            else
            {
                player.PrintToChat("Invalid amount. Please provide a number.");
            }
        }

        private void CommandTakeXp(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsAdmin) return;

            if (command.ArgCount < 3)
            {
                player.PrintToChat("Usage: !takexp <player> <amount>");
                return;
            }

            var targetPlayer = command.GetArg(1);
            var amount = command.GetArg(2);

            if (int.TryParse(amount, out int xpAmount))
            {
                var target = Utilities.GetPlayerFromName(targetPlayer);
                if (target != null)
                {
                    var steamId = target.SteamID;
                    if (_playerData.TryGetValue(steamId, out var data))
                    {
                        data.XP = Math.Max(0, data.XP - xpAmount);
                        player.PrintToChat($"Took {xpAmount} XP from {targetPlayer}.");
                        UpdatePlayerData(steamId, data);
                    }
                }
                else
                {
                    player.PrintToChat("Player not found.");
                }
            }
            else
            {
                player.PrintToChat("Invalid amount. Please provide a number.");
            }
        }

        private void CommandLevelUp(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsAdmin) return;

            if (command.ArgCount < 2)
            {
                player.PrintToChat("Usage: !levelup <player>");
                return;
            }

            var targetPlayer = command.GetArg(1);
            var target = Utilities.GetPlayerFromName(targetPlayer);
            if (target != null)
            {
                var steamId = target.SteamID;
                if (_playerData.TryGetValue(steamId, out var data))
                {
                    data.Level++;
                    player.PrintToChat($"Leveled up {targetPlayer} to level {data.Level}.");
                    UpdateCosmetics(target, data);
                    UpdatePlayerData(steamId, data);
                }
            }
            else
            {
                player.PrintToChat("Player not found.");
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

        private void CommandReset(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsAdmin) return;

            if (command.ArgCount < 2)
            {
                player.PrintToChat("Usage: !reset <player>");
                return;
            }

            var targetPlayer = command.GetArg(1);
            var target = Utilities.GetPlayerFromName(targetPlayer);
            if (target != null)
            {
                var steamId = target.SteamID;
                if (_playerData.TryGetValue(steamId, out var data))
                {
                    data.XP = 0;
                    data.Level = 1;
                    data.Prestige = 0;
                    data.ScoreboardTag = "Rookie";
                    data.ChatColor = "White";
                    player.PrintToChat($"Reset data for {targetPlayer}.");
                    UpdateCosmetics(target, data);
                    UpdatePlayerData(steamId, data);
                }
            }
            else
            {
                player.PrintToChat("Player not found.");
            }
        }

        private void CommandHelp(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            player.PrintToChat("CS2 Ranking Plugin Commands:");
            player.PrintToChat("!rank - Display player rank");
            player.PrintToChat("!top - Display top players");
            player.PrintToChat("!prestige - Prestige player");
            player.PrintToChat("!stats - Display player statistics");
            player.PrintToChat("!help - Display this help message");

            if (player.IsAdmin)
            {
                player.PrintToChat("Admin Commands:");
                player.PrintToChat("!config <setting> <value> - Configure plugin settings");
                player.PrintToChat("!givexp <player> <amount> - Give XP to a player");
                player.PrintToChat("!takexp <player> <amount> - Take XP from a player");
                player.PrintToChat("!levelup <player> - Level up a player");
                player.PrintToChat("!reset <player> - Reset player data");
            }
        }

        private void CommandAchievements(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            var steamId = player.SteamID;
            if (_playerData.TryGetValue(steamId, out var data))
            {
                player.PrintToChat($"Achievements for {player.PlayerName}:");
                player.PrintToChat($"Level {data.Level} - Current Level");
                player.PrintToChat($"Prestige {data.Prestige} - Current Prestige");
                player.PrintToChat($"XP {data.XP} - Current XP");
                player.PrintToChat($"Scoreboard Tag: {data.ScoreboardTag}");
                player.PrintToChat($"Chat Color: {data.ChatColor}");
            }
            else
            {
                player.PrintToChat("No achievements found.");
            }
        }

        private void CommandLeaderboard(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            player.PrintToChat("Player Leaderboard:");
            var sortedPlayers = _playerData.OrderByDescending(p => p.Value.Level).ThenByDescending(p => p.Value.XP).Take(10);
            int rank = 1;
            foreach (var kvp in sortedPlayers)
            {
                var playerName = Utilities.GetPlayerFromSteamId(kvp.Key)?.PlayerName ?? "Unknown";
                player.PrintToChat($"{rank}. {playerName} - Level {kvp.Value.Level}, XP {kvp.Value.XP}");
                rank++;
            }
        }

        private void CommandProgress(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            var steamId = player.SteamID;
            if (_playerData.TryGetValue(steamId, out var data))
            {
                player.PrintToChat($"Progress for {player.PlayerName}:");
                player.PrintToChat($"Level {data.Level} - Current Level");
                player.PrintToChat($"XP {data.XP} - Current XP");
                player.PrintToChat($"Prestige {data.Prestige} - Current Prestige");
                player.PrintToChat($"Scoreboard Tag: {data.ScoreboardTag}");
                player.PrintToChat($"Chat Color: {data.ChatColor}");
            }
            else
            {
                player.PrintToChat("No progress found.");
            }
        }

        private void CommandRewards(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            var steamId = player.SteamID;
            if (_playerData.TryGetValue(steamId, out var data))
            {
                player.PrintToChat($"Rewards for {player.PlayerName}:");
                player.PrintToChat($"Level {data.Level} - Current Level");
                player.PrintToChat($"Prestige {data.Prestige} - Current Prestige");
                player.PrintToChat($"Scoreboard Tag: {data.ScoreboardTag}");
                player.PrintToChat($"Chat Color: {data.ChatColor}");
            }
            else
            {
                player.PrintToChat("No rewards found.");
            }
        }

        private void CommandSettings(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            player.PrintToChat("Plugin Settings:");
            player.PrintToChat($"Kill XP: {_killXp}");
            player.PrintToChat($"Round Win XP: {_roundWinXp}");
            player.PrintToChat($"Level Up XP Multiplier: {_levelUpXpMultiplier}");
        }

        private void CommandVersion(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            player.PrintToChat($"CS2 Ranking Plugin Version: {ModuleVersion}");
        }

        private void CommandInfo(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            var steamId = player.SteamID;
            if (_playerData.TryGetValue(steamId, out var data))
            {
                player.PrintToChat($"Player Info for {player.PlayerName}:");
                player.PrintToChat($"Level: {data.Level}");
                player.PrintToChat($"Prestige: {data.Prestige}");
                player.PrintToChat($"XP: {data.XP}");
                player.PrintToChat($"Scoreboard Tag: {data.ScoreboardTag}");
                player.PrintToChat($"Chat Color: {data.ChatColor}");
            }
            else
            {
                player.PrintToChat("No info found.");
            }
        }

        private void CommandStatus(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            var steamId = player.SteamID;
            if (_playerData.TryGetValue(steamId, out var data))
            {
                player.PrintToChat($"Status for {player.PlayerName}:");
                player.PrintToChat($"Level: {data.Level}");
                player.PrintToChat($"Prestige: {data.Prestige}");
                player.PrintToChat($"XP: {data.XP}");
                player.PrintToChat($"Scoreboard Tag: {data.ScoreboardTag}");
                player.PrintToChat($"Chat Color: {data.ChatColor}");
            }
            else
            {
                player.PrintToChat("No status found.");
            }
        }

        private void CommandProfile(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            var steamId = player.SteamID;
            if (_playerData.TryGetValue(steamId, out var data))
            {
                player.PrintToChat($"Profile for {player.PlayerName}:");
                player.PrintToChat($"Level: {data.Level}");
                player.PrintToChat($"Prestige: {data.Prestige}");
                player.PrintToChat($"XP: {data.XP}");
                player.PrintToChat($"Scoreboard Tag: {data.ScoreboardTag}");
                player.PrintToChat($"Chat Color: {data.ChatColor}");
            }
            else
            {
                player.PrintToChat("No profile found.");
            }
        }

        private void CommandHistory(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            var steamId = player.SteamID;
            if (_playerData.TryGetValue(steamId, out var data))
            {
                player.PrintToChat($"History for {player.PlayerName}:");
                player.PrintToChat($"Level: {data.Level}");
                player.PrintToChat($"Prestige: {data.Prestige}");
                player.PrintToChat($"XP: {data.XP}");
                player.PrintToChat($"Scoreboard Tag: {data.ScoreboardTag}");
                player.PrintToChat($"Chat Color: {data.ChatColor}");
            }
            else
            {
                player.PrintToChat("No history found.");
            }
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var attacker = @event.Attacker;
            if (attacker != null && attacker.IsValid)
            {
                var steamId = attacker.SteamID;
                if (_playerData.TryGetValue(steamId, out var data))
                {
                    data.XP += _killXp; // Use configurable XP
                    CheckLevelUp(attacker, data);
                    UpdatePlayerData(steamId, data);
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            var winner = @event.Winner;
            if (winner != null)
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    if (player.Team == winner)
                    {
                        var steamId = player.SteamID;
                        if (_playerData.TryGetValue(steamId, out var data))
                        {
                            data.XP += _roundWinXp; // Use configurable XP
                            CheckLevelUp(player, data);
                            UpdatePlayerData(steamId, data);
                        }
                    }
                }
            }
            return HookResult.Continue;
        }

        private void CheckLevelUp(CCSPlayerController player, PlayerData data)
        {
            if (data.XP >= data.Level * _levelUpXpMultiplier) // Use configurable multiplier
            {
                data.Level++;
                player.PrintToChat($"Level Up! You are now level {data.Level}.");
                UpdateCosmetics(player, data);
            }
        }

        private void UpdateCosmetics(CCSPlayerController player, PlayerData data)
        {
            // Example: Set scoreboard tag based on level and prestige
            data.ScoreboardTag = $"[P{data.Prestige} L{data.Level}]";

            // Example: Set chat color based on prestige
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

            // Apply cosmetics (placeholder for actual implementation)
            player.PrintToChat($"Cosmetics updated: {data.ScoreboardTag} with {data.ChatColor} chat color.");

            // Example: Apply PNG prestige icon (placeholder)
            ApplyPrestigeIcon(player, data.Prestige);
        }

        private void ApplyPrestigeIcon(CCSPlayerController player, int prestige)
        {
            // Placeholder for actual PNG icon implementation
            // This would typically involve loading a PNG file and applying it to the player's scoreboard or chat
            player.PrintToChat($"Prestige Icon applied: Prestige {prestige} (PNG icon placeholder)");
        }

        private void UpdatePlayerData(ulong steamId, PlayerData data)
        {
            using (var connection = new MySqlConnection(_connectionString))
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

        private HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo info)
        {
            var player = @event.Player;
            if (player != null)
            {
                LoadPlayerData(player);
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Player;
            if (player != null)
            {
                SavePlayerData(player);
            }
            return HookResult.Continue;
        }

        private void LoadPlayerData(CCSPlayerController player)
        {
            var steamId = player.SteamID;
            using (var connection = new MySqlConnection(_connectionString))
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