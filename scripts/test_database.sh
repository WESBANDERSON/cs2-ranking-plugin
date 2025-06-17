#!/bin/bash

# Configuration
DB_HOST="rkb.site.nfoservers.com"
DB_PORT="3306"
DB_NAME="rkb_vanity"
DB_USER="rkb"
DB_PASS="dD7xPpfyfd"

echo "Testing database connection..."
if mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "SELECT 1" >/dev/null 2>&1; then
    echo "✅ Database connection successful"
else
    echo "❌ Database connection failed"
    exit 1
fi

echo "Checking database schema..."
if mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "DESCRIBE players" >/dev/null 2>&1; then
    echo "✅ Players table exists"
else
    echo "❌ Players table not found"
    exit 1
fi

echo "Testing table structure..."
COLUMNS=$(mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "SHOW COLUMNS FROM players" | wc -l)
if [ "$COLUMNS" -ge 17 ]; then
    echo "✅ Table structure is correct"
else
    echo "❌ Table structure is incomplete"
    exit 1
fi

echo "Testing indexes..."
INDEXES=$(mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "SHOW INDEX FROM players" | wc -l)
if [ "$INDEXES" -ge 5 ]; then
    echo "✅ Indexes are properly set up"
else
    echo "❌ Indexes are missing"
    exit 1
fi

echo "Testing data insertion..."
mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "
INSERT INTO players (steam_id, xp, level, prestige, scoreboard_tag, chat_color)
VALUES (76561198012345678, 1000, 1, 0, 'Rookie', 'White')
ON DUPLICATE KEY UPDATE xp = 1000;"

if [ $? -eq 0 ]; then
    echo "✅ Data insertion successful"
else
    echo "❌ Data insertion failed"
    exit 1
fi

echo "Testing data retrieval..."
RESULT=$(mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "
SELECT COUNT(*) FROM players WHERE steam_id = 76561198012345678;" | tail -n 1)

if [ "$RESULT" -eq 1 ]; then
    echo "✅ Data retrieval successful"
else
    echo "❌ Data retrieval failed"
    exit 1
fi

echo "All tests completed successfully! 🎉" 