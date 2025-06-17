-- Add new columns for player statistics if they do not exist
SET @table := 'players';

-- Helper procedure to add a column if it does not exist
DELIMITER //
CREATE PROCEDURE add_column_if_not_exists(IN tbl VARCHAR(64), IN col VARCHAR(64), IN coldef TEXT)
BEGIN
    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE table_name=tbl AND column_name=col AND table_schema=DATABASE()
    ) THEN
        SET @ddl = CONCAT('ALTER TABLE ', tbl, ' ADD COLUMN ', col, ' ', coldef);
        PREPARE stmt FROM @ddl;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END //
DELIMITER ;

CALL add_column_if_not_exists(@table, 'total_kills', 'INT DEFAULT 0');
CALL add_column_if_not_exists(@table, 'total_deaths', 'INT DEFAULT 0');
CALL add_column_if_not_exists(@table, 'total_assists', 'INT DEFAULT 0');
CALL add_column_if_not_exists(@table, 'total_rounds_won', 'INT DEFAULT 0');
CALL add_column_if_not_exists(@table, 'total_rounds_lost', 'INT DEFAULT 0');
CALL add_column_if_not_exists(@table, 'total_mvps', 'INT DEFAULT 0');
CALL add_column_if_not_exists(@table, 'total_bombs_planted', 'INT DEFAULT 0');
CALL add_column_if_not_exists(@table, 'total_bombs_defused', 'INT DEFAULT 0');
CALL add_column_if_not_exists(@table, 'last_seen', 'TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP');

DROP PROCEDURE IF EXISTS add_column_if_not_exists;

-- Create indexes for better performance (ignore errors if they exist)
CREATE INDEX idx_level ON players(level);
CREATE INDEX idx_prestige ON players(prestige);
CREATE INDEX idx_xp ON players(xp);
CREATE INDEX idx_last_seen ON players(last_seen);

-- Update default values for existing columns
UPDATE players SET scoreboard_tag = 'Rookie' WHERE scoreboard_tag IS NULL OR scoreboard_tag = '';
UPDATE players SET chat_color = 'White' WHERE chat_color IS NULL OR chat_color = ''; 