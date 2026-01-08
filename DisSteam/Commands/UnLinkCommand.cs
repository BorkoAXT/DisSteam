using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DisSteam.Data;

namespace DisSteam.Commands
{
    public sealed class UnlinkCommand : ApplicationCommandModule
    {
        private static readonly LinkStore _store = new LinkStore("links.db");

        [SlashCommand("unlink", "Unlinks your Steam account from your Discord account.")]
        public async Task Unlink(InteractionContext context)
        {
            await context.CreateResponseAsync(
                DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            var steamId64 = _store.GetSteamId64(context.User.Id);
            if (steamId64 == null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("You have no linked Steam account."));

                await Task.Delay(5000);
                await context.DeleteResponseAsync();

                return;
            }

            if (!_store.Unlink(context.User.Id))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Failed to unlink your Steam account. Please try again."));

                await Task.Delay(5000);
                await context.DeleteResponseAsync();

                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Steam account unlinked")
                .AddField("SteamID64", steamId64, true)
                .WithFooter($"Requested by {context.User.Username}");

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().AddEmbed(embed));

            await Task.Delay(5000);
            await context.DeleteResponseAsync();
        }
    }
}