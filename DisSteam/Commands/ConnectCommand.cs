using DisSteam.Data;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Steam.Models.SteamCommunity;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;

namespace DisSteam.Commands
{
    public sealed class ConnectCommand : ApplicationCommandModule
    {
        private static readonly LinkStore _store = new LinkStore("links.db");
        private static readonly string? SteamAPIKey = Environment.GetEnvironmentVariable("STEAM_API_KEY");
        private static readonly SteamWebInterfaceFactory _factory =
            new SteamWebInterfaceFactory(SteamAPIKey);

        [SlashCommand("connect", "Connects a discord account to a steam account")]
        public async Task Connect(
            InteractionContext context,
            [Option("steamid", "Your SteamID64")] string steamId)
        {
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            if (string.IsNullOrWhiteSpace(SteamAPIKey))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Steam API key is not configured."));
                return;
            }

            if (!ulong.TryParse(steamId, out var steamId64))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("SteamID must be a number."));
                return;
            }

            if (steamId64 < 76561190000000000UL)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid SteamID64."));
                return;
            }

            var existingSteamIdForUser = _store.GetSteamId64(context.User.Id);
            if (existingSteamIdForUser != null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"You already linked SteamID64 `{existingSteamIdForUser}`."));
                return;
            }

            if (_store.SteamIdExists(steamId64.ToString()))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("That SteamID64 is already linked to another Discord user."));
                return;
            }

            PlayerSummaryModel? summary;
            try
            {
                var steamUser = _factory.CreateSteamWebInterface<SteamUser>(new HttpClient());
                var resp = await steamUser.GetPlayerSummaryAsync(steamId64);

                summary = resp?.Data;
            }
            catch (Exception)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Steam request failed (network/API error)."));
                return;
            }

            if (summary == null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("No Steam user found with that SteamID64."));
                return;
            }

            var personaName = string.IsNullOrWhiteSpace(summary.Nickname) ? "Unknown" : summary.Nickname;
            var profileUrl = summary.ProfileUrl ?? string.Empty;
            var avatarUrl = summary.AvatarFullUrl ?? string.Empty;

            _store.UpsertLink(
                context.User.Id,
                steamId64,
                personaName,
                profileUrl,
                avatarUrl
            );

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

