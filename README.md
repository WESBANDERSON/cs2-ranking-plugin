# CS2 Ranking Plugin

A vanity-driven ranking system for Counter-Strike 2 servers with in-game cosmetic rewards.

## Features

- XP-based ranking system
- Prestige levels
- Custom scoreboard tags
- Custom chat colors
- MySQL database integration
- Top players leaderboard

## Requirements

- Counter-Strike 2 server
- .NET 7.0 SDK
- MySQL database (already set up at rkb.site.nfoservers.com)

## Installation

1. Clone this repository:
```bash
git clone https://github.com/WESBANDERSON/cs2-ranking-plugin.git
cd cs2-ranking-plugin
```

2. Build the plugin:
```bash
dotnet build
```

3. Create the plugin directory on your CS2 server:
```bash
mkdir -p csgo/addons/cs2-ranking
```

4. Copy the built plugin:
```bash
cp bin/Debug/net7.0/CS2RankingPlugin.dll csgo/addons/cs2-ranking/
```

5. Create the config file:
```bash
mkdir -p csgo/addons/cs2-ranking
```

Create `csgo/addons/cs2-ranking/config.json` with your database settings:
```json
{
  "DatabaseHost": "rkb.site.nfoservers.com",
  "DatabasePort": 3306,
  "DatabaseName": "rkb_vanity",
  "DatabaseUser": "rkb",
  "DatabasePassword": "dD7xPpfyfd"
}
```

6. Add the plugin to your server's `plugins.json`:
```json
{
  "EnabledPlugins": [
    "CS2RankingPlugin"
  ]
}
```

## Database

The database is already set up at rkb.site.nfoservers.com with:
- Database name: rkb_vanity
- Tables: players (with all necessary fields)
- View: top_players

## Commands

- `!rank` - Display your current rank and XP
- `!top` - Show the top 10 players
- `!prestige` - Prestige your rank (available at level 100)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 