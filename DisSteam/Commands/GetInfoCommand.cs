using System.Text.Json;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using RestSharp;
using Steam.Models.SteamCommunity;

namespace DisSteam.Commands
{
    public sealed class GetInfoCommand : ApplicationCommandModule
    {
        [SlashCommand("connect", "Connects a discord account to a steam account")]
        public async Task Connect(
            InteractionContext context,
            [Option("steamid", "Your SteamID64")] string steamId)
        {
            await context.CreateResponseAsync(
                DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            if (!long.TryParse(steamId, out var steamId64))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("SteamID must be a number."));
                return;
            }

            if (steamId64 < 76561190000000000L)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid SteamID64."));
                return;
            }

            RestClient client = new RestClient();

            RestRequest request = new RestRequest("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/", Method.Get);

            request.AddQueryParameter("key", "37C29A3EDF64E8060AAB992E13D96FC8");
            request.AddQueryParameter("steamids", steamId);

            RestResponse response;
            try
            {
                response = await client.ExecuteAsync(request);
            }
            catch
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Steam request failed (network error)."));
                return;
            }

            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"Steam API error: {(int)response.StatusCode} {response.StatusDescription}"));
                return;
            }

            using JsonDocument document = JsonDocument.Parse(response.Content);

            var players = document.RootElement.GetProperty("response").GetProperty("players");
            if (players.GetArrayLength() == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("No Steam user found with that SteamID64."));
                return;
            }

            var player = players[0];
            string personaName = player.GetProperty("personaname").GetString() ?? "Unknown";
            string profileUrl = player.GetProperty("profileurl").GetString() ?? "Unknown";
            string avatar = player.TryGetProperty("avatarfull", out var av) ? (av.GetString() ?? "") : "";

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle($"Found Steam profile: {personaName}")
                .WithUrl(profileUrl)
                .WithThumbnail(avatar)
                .AddField("SteamID64", steamId, true)
                .WithFooter($"Requested by {context.User.Username}");

            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(embed));
        }
    }
}
