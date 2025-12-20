using DisSteam.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace DisSteam.Commands
{
    public sealed class WhoAmICommand : ApplicationCommandModule
    {
        private static readonly LinkStore _store = new LinkStore("links.db");

        [SlashCommand("whoami", "Shows the Steam profile linked to your Discord account.")]
        public async Task WhoAmI(InteractionContext context)
        {
            await context.CreateResponseAsync(
                DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            var link = _store.GetLink(context.User.Id);
            if (link == null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("You have not linked a Steam account yet."));
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Found Steam profile")
                .WithDescription(link.PersonaName)
                .WithUrl(string.IsNullOrWhiteSpace(link.ProfileUrl) ? null : link.ProfileUrl)
                .WithThumbnail(string.IsNullOrWhiteSpace(link.AvatarUrl) ? null : link.AvatarUrl)
                .AddField("SteamID64", link.SteamId64, true)
                .WithFooter($"Requested by {context.User.Username}");

            var steamProfileLink = new DiscordLinkButtonComponent(
                url: link.ProfileUrl,
                label: "Open Steam Profile",
                disabled: string.IsNullOrWhiteSpace(link.ProfileUrl),
                emoji: new DiscordComponentEmoji("🎮"));

            var steamRepLink = new DiscordLinkButtonComponent(
                url: $"https://steamladder.com/profile/{link.SteamId64}",
                label: "View on SteamLadder",
                disabled: false,
                emoji: new DiscordComponentEmoji("🔍"));

            var unlinkButton = new DiscordButtonComponent(
                ButtonStyle.Danger,
                customId: $"find:unlink:{context.User.Id}",
                label: "Unlink (self)",
                disabled: false,
                emoji: new DiscordComponentEmoji("✖️"));

            await context.EditResponseAsync(new DiscordWebhookBuilder().AddComponents(steamProfileLink, steamRepLink, unlinkButton).AddEmbed(embed));

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().AddComponents(steamProfileLink, steamRepLink, unlinkButton).AddEmbed(embed));
        }
    }
}