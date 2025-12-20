using DisSteam.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace DisSteam
{
    public static class UnlinkButtonHandler
    {
        private static readonly LinkStore _store = new LinkStore("links.db");

        public static void Register(DiscordClient client)
        {
            client.ComponentInteractionCreated += OnComponentInteractionCreated;
        }

        private static async Task OnComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs e)
        {
            if (!e.Id.StartsWith("find:unlink:", StringComparison.Ordinal))
                return;

            var parts = e.Id.Split(':');
            if (parts.Length != 3 || !ulong.TryParse(parts[2], out var targetUserId))
            {
                await e.Interaction.CreateResponseAsync(
                    InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("❌ Invalid button payload.")
                        .AsEphemeral());
                return;
            }
            if (e.User.Id != targetUserId)
            {
                await e.Interaction.CreateResponseAsync(
                    InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("❌ You can only unlink your own Steam account.")
                        .AsEphemeral());
                return;
            }
            
            bool removed = _store.Unlink(e.User.Id);

            await e.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent(removed
                        ? "✅ Your Steam account has been unlinked."
                        : "❌ You aren't linked to a steam account.")
                    .AsEphemeral());
        }
    }
}
