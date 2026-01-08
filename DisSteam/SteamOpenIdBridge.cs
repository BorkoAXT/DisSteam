using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using Steam.Models.SteamCommunity;
using DisSteam.Data;

namespace DisSteam
{
    public static class SteamOpenIdBridge
    {
        private static readonly LinkStore _store = new LinkStore("links.db");

        private static readonly Dictionary<string, ulong> Pending = new();
        private static readonly object LockObj = new();

        private static readonly string? SteamAPIKey = Environment.GetEnvironmentVariable("STEAM_API_KEY");
        private static readonly SteamWebInterfaceFactory _factory = new SteamWebInterfaceFactory(SteamAPIKey);

        public static void RegisterPending(string state, ulong discordId)
        {
            lock (LockObj)
                Pending[state] = discordId;
        }

        public static async Task<bool> CompleteAsync(string state, string steamId64)
        {
            ulong discordId;
            lock (LockObj)
            {
                if (!Pending.TryGetValue(state, out discordId))
                    return false;

                Pending.Remove(state);
            }
            if (_store.SteamIdExists(steamId64))
                return false;

            string personaName = "Unknown";
            string profileUrl = "";
            string avatarUrl = "";

            if (!string.IsNullOrWhiteSpace(SteamAPIKey) && ulong.TryParse(steamId64, out var sid))
            {
                try
                {
                    var steamUser = _factory.CreateSteamWebInterface<SteamUser>(new HttpClient());
                    var resp = await steamUser.GetPlayerSummaryAsync(sid);
                    PlayerSummaryModel? summary = resp?.Data;
                    if (summary != null)
                    {
                        personaName = string.IsNullOrWhiteSpace(summary.Nickname) ? "Unknown" : summary.Nickname;
                        profileUrl = summary.ProfileUrl ?? "";
                        avatarUrl = summary.AvatarFullUrl ?? "";
                    }
                }
                catch
                {

                }
            }

            _store.UpsertLink(discordId, ulong.Parse(steamId64), personaName, profileUrl, avatarUrl);
            return true;
        }
    }
}
