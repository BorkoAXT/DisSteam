using DisSteam.Data;
using DSharpPlus;
using DSharpPlus.Entities;

namespace DisSteam.Views
{
    public static class FindView
    {
        private static readonly LinkStore _store = new LinkStore("links.db");

        public static DiscordInteractionResponseBuilder Build(
     DiscordUser requester,
     DiscordUser target)
        {
            var steamUser = _store.GetLink(target.Id);

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Found Steam profile")
                .WithDescription(steamUser.PersonaName)
                .WithUrl(string.IsNullOrWhiteSpace(steamUser.ProfileUrl) ? null : steamUser.ProfileUrl)
                .WithThumbnail(string.IsNullOrWhiteSpace(steamUser.AvatarUrl) ? null : steamUser.AvatarUrl)
                .AddField("SteamID64", steamUser.SteamId64, true)
                .WithFooter($"Requested by {requester.Username}");

            var steamProfileLink = new DiscordLinkButtonComponent(
                steamUser.ProfileUrl,
                "Open Steam Profile",
                false,
                new DiscordComponentEmoji("🎮"));

            var steamRepLink = new DiscordLinkButtonComponent(
                $"https://steamladder.com/profile/{steamUser.SteamId64}",
                "View on SteamLadder",
                false,
                new DiscordComponentEmoji("🔍"));

            var unlinkButton = new DiscordButtonComponent( 
                ButtonStyle.Danger, 
                customId: $"find:unlink:{target.Id}", 
                label: "Unlink (self)", 
                disabled: false, 
                emoji: new DiscordComponentEmoji("✖️"));

            var infoButton = new DiscordButtonComponent(
                ButtonStyle.Primary,
                customId: $"find:info:{target.Id}",
                label: "More info",
                emoji: new DiscordComponentEmoji("ℹ️"));

            return new DiscordInteractionResponseBuilder()
                .AddEmbed(embed)
                .AddComponents(steamProfileLink, steamRepLink, unlinkButton, infoButton);
        }
    }
}
