# TABServerName

*Read this in [Turkish / Türkçe](README.tr.md).*

**This plugin has been discontinued. Use this one instead:**

### https://github.com/qstage/cs2_change_scoreboard_mapname

It does the same job (adding your own text next to the map name on the TAB scoreboard), and it does it properly:

- No extra dependencies
- Works at the net message level, so the change is only seen by the player
- The server information and the map name in the Steam server browser stay correct

## Why It Was Removed

TABServerName used to write over the server's own map name. That made the map name look broken in the Steam server browser, and other plugins reading the map name were seeing the fake one.