# CS2 Ranking Plugin

A Counter-Strike 2 server plugin that implements a vanity-driven ranking system with in-game cosmetic rewards.

## Overview

This plugin provides a ranking system similar to FaceIt/ESEA but entirely within CS2, featuring:

- Player XP, rank, and prestige progression (1-55 levels, 10 prestiges)
- In-game cosmetic rewards (scoreboard tags, chat colors, prestige icons)
- Command-based configuration (with future web interface support)
- MySQL database for persistent player data

## Setup

### Prerequisites

- CounterStrikeSharp (C#) installed on your CS2 server
- MySQL database (created and configured)

### Database Configuration

- Database: `cs2_ranking`
- User: `cs2_user`
- Password: `cs2_password`

### Installation

1. Clone this repository to your CS2 server's plugins directory.
2. Configure the MySQL connection in the plugin's config file.
3. Restart your CS2 server.

## Next Steps

- Implement the plugin skeleton in C# using CounterStrikeSharp.
- Set up the MySQL connection and player data tables.
- Develop the XP, rank, and prestige logic.
- Add cosmetic rewards (scoreboard tags, chat colors, prestige icons).
- Create command-based configuration system.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details. 