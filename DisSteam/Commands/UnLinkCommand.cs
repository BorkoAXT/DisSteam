using DisSteam.Data;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace DisSteam.Commands
{
    public sealed class UnlinkCommand : ApplicationCommandModule
    {
        private static readonly LinkStore _store = new LinkStore("links.db");

        [SlashCommand("unlink", "Unlinks your Steam account from your Discord account.")]
        public async Task Unlink(InteractionContext context)
        {
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            var existing = _store.GetSteamId64(context.User.Id);
            if (existing == null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You have no linked Steam account."));
                return;
            }

            bool removed = _store.Unlink(context.User.Id);
            if (!removed)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Failed to unlink. Please try again."));
                return;
            }

            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Unlinked SteamID64 `{existing}`."));
        }
    }
}