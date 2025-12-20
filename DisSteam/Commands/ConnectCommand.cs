using System.Text.Json;
using DisSteam.Data;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using RestSharp;

namespace DisSteam.Commands
{
    public sealed class ConnectCommand : ApplicationCommandModule
    {
        private static readonly LinkStore _store = new LinkStore("links.db");
        private static readonly string SteamAPIKey = Environment.GetEnvironmentVariable("STEAM_API_KEY");

        [SlashCommand("connect", "Connects a discord account to a steam account")]
        public async Task Connect(
            InteractionContext context,
            [Option("steamid", "Your SteamID64")] string steamId)
        {
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            if (string.IsNullOrWhiteSpace(SteamAPIKey))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Steam API key is not configured."));
                return;
            }

            if (!long.TryParse(steamId, out var steamId64))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("SteamID must be a number."));
                return;
            }

            if (steamId64 < 76561190000000000L)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Invalid SteamID64."));
                return;
            }

            var existingSteamIdForUser = _store.GetSteamId64(context.User.Id);
            if (existingSteamIdForUser != null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"You already linked SteamID64 `{existingSteamIdForUser}`."));
                return;
            }

            if (_store.SteamIdExists(steamId64.ToString()))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("That SteamID64 is already linked to another Discord user."));
                return;
            }

            var client = new RestClient();
            var request = new RestRequest("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/", Method.Get);
            request.AddQueryParameter("key", SteamAPIKey);
            request.AddQueryParameter("steamids", steamId64.ToString());

            RestResponse response;
            try
            {
                response = await client.ExecuteAsync(request);
            }
            catch
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Steam request failed (network error)."));
                return;
            }

            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Steam API error: {(int)response.StatusCode} {response.StatusDescription}"));
                return;
            }

            using var document = JsonDocument.Parse(response.Content);
            var players = document.RootElement.GetProperty("response").GetProperty("players");

            if (players.GetArrayLength() == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No Steam user found with that SteamID64."));
                return;
            }

            var player = players[0];
            string personaName = player.GetProperty("personaname").GetString() ?? "Unknown";
            string profileUrl = player.GetProperty("profileurl").GetString() ?? "";
            string avatarUrl = player.TryGetProperty("avatarfull", out var av) ? (av.GetString() ?? "") : "";

            _store.UpsertLink(context.User.Id, (ulong)steamId64, personaName, profileUrl, avatarUrl);

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Linked Steam profile: {personaName}")
                .WithUrl(string.IsNullOrWhiteSpace(profileUrl) ? null : profileUrl)
                .WithThumbnail(string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl)
                .AddField("SteamID64", steamId64.ToString(), true)
                .WithFooter($"Requested by {context.User.Username}");

            await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
