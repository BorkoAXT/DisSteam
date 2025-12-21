using DisSteam.Data;
using DisSteam.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;

namespace DisSteam.Commands
{
    public sealed class FindCommand : ApplicationCommandModule
    {
        private static readonly LinkStore _store = new LinkStore("links.db");

        [SlashCommand("find", "Finds the Steam profile of a user")]
        public async Task Find(
            InteractionContext context,
            [Option("user", "The user")] DiscordUser user)
        {
            await context.CreateResponseAsync(
                DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            var steamId = _store.GetSteamId64(user.Id);
            if (steamId == null)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder()
                        .WithContent("This user hasn't linked their Discord account with Steam."));
                return;
            }

            BasicSteamUser steamUser = _store.GetLink(user.Id);
            if (steamUser.PersonaName == _store.GetLink(context.User.Id).PersonaName)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Found Steam profile")
                    .WithDescription(steamUser.PersonaName)
                    .WithUrl(string.IsNullOrWhiteSpace(steamUser.ProfileUrl) ? null : steamUser.ProfileUrl)
                    .WithThumbnail(string.IsNullOrWhiteSpace(steamUser.AvatarUrl) ? null : steamUser.AvatarUrl)
                    .AddField("SteamID64", steamUser.SteamId64, true)
                    .WithFooter($"Requested by {context.User.Username}");

                var steamProfileLink = new DiscordLinkButtonComponent(
                    url: steamUser.ProfileUrl,
                    label: "Open Steam Profile",
                    disabled: string.IsNullOrWhiteSpace(steamUser.ProfileUrl),
                    emoji: new DiscordComponentEmoji("🎮"));

                var steamRepLink = new DiscordLinkButtonComponent(
                    url: $"https://steamladder.com/profile/{steamUser.SteamId64}",
                    label: "View on SteamLadder",
                    disabled: false,
                    emoji: new DiscordComponentEmoji("🔍"));

                var unlinkButton = new DiscordButtonComponent(
                    ButtonStyle.Danger,
                    customId: $"find:unlink:{user.Id}",
                    label: "Unlink (self)",
                    disabled: false,
                    emoji: new DiscordComponentEmoji("✖️"));

                var infoButton = new DiscordButtonComponent(
                    ButtonStyle.Primary,
                    customId: $"find:info:{user.Id}",
                    label: "More info",
                    emoji: new DiscordComponentEmoji("ℹ️"));

                await context.EditResponseAsync(new DiscordWebhookBuilder().AddComponents(steamProfileLink, steamRepLink, unlinkButton, infoButton).AddEmbed(embed));
            }
            else
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Found Steam profile")
                    .WithDescription(steamUser.PersonaName)
                    .WithUrl(string.IsNullOrWhiteSpace(steamUser.ProfileUrl) ? null : steamUser.ProfileUrl)
                    .WithThumbnail(string.IsNullOrWhiteSpace(steamUser.AvatarUrl) ? null : steamUser.AvatarUrl)
                    .AddField("SteamID64", steamUser.SteamId64, true)
                    .WithFooter($"Requested by {context.User.Username}");

                var steamProfileLink = new DiscordLinkButtonComponent(
                    url: steamUser.ProfileUrl,
                    label: "Open Steam Profile",
                    disabled: string.IsNullOrWhiteSpace(steamUser.ProfileUrl),
                    emoji: new DiscordComponentEmoji("🎮"));

                var steamRepLink = new DiscordLinkButtonComponent(
                    url: $"https://steamladder.com/profile/{steamUser.SteamId64}",
                    label: "View on SteamLadder",
                    disabled: false,
                    emoji: new DiscordComponentEmoji("🔍"));

                var infoButton = new DiscordButtonComponent(
                    ButtonStyle.Primary,
                    customId: $"find:info:{user.Id}",
                    label: "More info",
                    emoji: new DiscordComponentEmoji("ℹ️"));

                await context.EditResponseAsync(new DiscordWebhookBuilder().AddComponents(steamProfileLink, steamRepLink, infoButton).AddEmbed(embed));
            }

            
        }
    }
}
