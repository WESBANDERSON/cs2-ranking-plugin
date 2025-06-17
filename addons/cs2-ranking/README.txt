CS2 Ranking Plugin

1. Copy the entire 'cs2-ranking' folder to your CS2 server's addons directory:
   csgo/addons/cs2-ranking/

2. Create config.json in the cs2-ranking folder with these settings:
{
  "DatabaseHost": "rkb.site.nfoservers.com",
  "DatabasePort": 3306,
  "DatabaseName": "rkb_vanity",
  "DatabaseUser": "rkb",
  "DatabasePassword": "dD7xPpfyfd"
}

3. Add the plugin to your server's plugins.json:
{
  "EnabledPlugins": [
    "CS2RankingPlugin"
  ]
}

The database is already set up and ready to use. 