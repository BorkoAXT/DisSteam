using DisSteam.Models;
using Microsoft.Data.Sqlite;

namespace DisSteam.Data
{
    public sealed class LinkStore
    {
        private readonly string _connString;

        public LinkStore(string dbFilePath = "links.db")
        {
            _connString = new SqliteConnectionStringBuilder
            {
                DataSource = dbFilePath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            Init();
        }

        private void Init()
        {
            using var con = new SqliteConnection(_connString);
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS user_links (
    discord_user_id INTEGER PRIMARY KEY,
    steam_id64      TEXT NOT NULL UNIQUE,
    linked_at       TEXT NOT NULL,
    verified        INTEGER NOT NULL DEFAULT 1,
    persona_name    TEXT,
    profile_url     TEXT,
    avatar_url      TEXT,
    last_refresh    TEXT
);";
            cmd.ExecuteNonQuery();
        }

        public string? GetSteamId64(ulong discordUserId)
        {
            using var con = new SqliteConnection(_connString);
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
SELECT steam_id64
FROM user_links
WHERE discord_user_id = $duid
LIMIT 1;";
            cmd.Parameters.AddWithValue("$duid", (long)discordUserId);

            return cmd.ExecuteScalar() as string;
        }

        public bool SteamIdExists(string steamId64)
        {
            using var con = new SqliteConnection(_connString);
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
SELECT 1
FROM user_links
WHERE steam_id64 = $sid
LIMIT 1;";
            cmd.Parameters.AddWithValue("$sid", steamId64);

            return cmd.ExecuteScalar() != null;
        }

        public void UpsertLink(ulong discordUserId, ulong steamId64, string personaName, string profileUrl, string avatarUrl)
        {
            using var con = new SqliteConnection(_connString);
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
INSERT INTO user_links (discord_user_id, steam_id64, linked_at, verified, persona_name, profile_url, avatar_url, last_refresh)
VALUES ($duid, $sid, datetime('now'), 1, $pname, $purl, $aurl, datetime('now'))
ON CONFLICT(discord_user_id) DO UPDATE SET
  steam_id64   = excluded.steam_id64,
  linked_at    = excluded.linked_at,
  verified     = 1,
  persona_name = excluded.persona_name,
  profile_url  = excluded.profile_url,
  avatar_url   = excluded.avatar_url,
  last_refresh = excluded.last_refresh;";
            cmd.Parameters.AddWithValue("$duid", (long)discordUserId);
            cmd.Parameters.AddWithValue("$sid", steamId64.ToString());
            cmd.Parameters.AddWithValue("$pname", personaName ?? "");
            cmd.Parameters.AddWithValue("$purl", profileUrl ?? "");
            cmd.Parameters.AddWithValue("$aurl", avatarUrl ?? "");

            cmd.ExecuteNonQuery();
        }

        public BasicSteamUser? GetLink(ulong discordUserId)
        {
            using var con = new SqliteConnection(_connString);
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
SELECT steam_id64, persona_name, profile_url, avatar_url
FROM user_links
WHERE discord_user_id = $duid
LIMIT 1;";
            cmd.Parameters.AddWithValue("$duid", (long)discordUserId);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            string sid = r.GetString(0);
            string pname = r.IsDBNull(1) ? "Unknown" : r.GetString(1);
            string purl = r.IsDBNull(2) ? "" : r.GetString(2);
            string aurl = r.IsDBNull(3) ? "" : r.GetString(3);

            return new BasicSteamUser(sid, pname, purl, aurl);
        }

        public bool Unlink(ulong discordUserId)
        {
            using var con = new SqliteConnection(_connString);
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM user_links WHERE discord_user_id = $duid;";
            cmd.Parameters.AddWithValue("$duid", (long)discordUserId);

            return cmd.ExecuteNonQuery() > 0;
        }
    }
}