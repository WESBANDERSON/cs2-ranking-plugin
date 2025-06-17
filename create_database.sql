-- Drop the database if it exists and create a new one
DROP DATABASE IF EXISTS cs2_ranking;
CREATE DATABASE cs2_ranking;

-- Use the database
USE cs2_ranking;

-- Create the players table
CREATE TABLE players (
    steam_id BIGINT PRIMARY KEY,
    xp INT DEFAULT 0,
    level INT DEFAULT 1,
    prestige INT DEFAULT 0,
    scoreboard_tag VARCHAR(50) DEFAULT 'Rookie',
    chat_color VARCHAR(50) DEFAULT 'White',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Create indexes for better performance
CREATE INDEX idx_level ON players(level);
CREATE INDEX idx_prestige ON players(prestige);
CREATE INDEX idx_xp ON players(xp);

-- Create a view for top players
CREATE VIEW top_players AS
SELECT 
    steam_id,
    level,
    prestige,
    xp,
    scoreboard_tag,
    chat_color
FROM players
ORDER BY prestige DESC, level DESC, xp DESC
LIMIT 100; 