# CS2 Ranking Plugin

A vanity-driven ranking system with in-game cosmetic rewards for Counter-Strike 2 servers.

## Features

- Player ranking system with levels and prestige
- XP rewards for kills, assists, round wins, and special actions
- Customizable scoreboard tags and chat colors
- Admin commands for XP management
- MySQL database integration

## Installation on NFO Servers

1. **Database Setup**
   - The plugin will automatically create the required database table on first run
   - Database credentials are pre-configured for NFO servers
   - Database name: `rkb_vanity`
   - Username: `rkb`
   - Password: `dD7xPpfyfd`
   - Host: `localhost`

2. **Plugin Installation**
   - Copy the compiled plugin to your server's `addons/cs2-ranking` directory
   - The plugin will create a `config.json` file on first run
   - Restart your server or reload the plugin

3. **Commands**
   - `!rank` - Display player rank
   - `!top` - Display top players
   - `!prestige` - Prestige player (requires level 55)
   - `!stats` - Display player statistics
   - Admin Commands:
     - `css_lr_giveexp <player> <amount>` - Give XP to a player
     - `css_lr_takeexp <player> <amount>` - Take XP from a player
     - `css_lr_enabled <0|1>` - Toggle ranking system

## Configuration
The plugin creates a `config.json` file in the `addons/cs2-ranking` directory. You can modify:
- XP rewards for different actions
- Minimum players required for XP gain
- Experience messages visibility
- Database connection settings

## Support
For support or issues, please contact the development team. 