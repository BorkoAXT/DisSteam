using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DisSteam.Data;

namespace DisSteam.Commands
{
    public sealed class ConnectCommand : ApplicationCommandModule
    {
        private static readonly LinkStore _store = new LinkStore("links.db");

        private static readonly string PublicUrl =
            "https://suggestion-photographs-comparisons-happens.trycloudflare.com";

        [SlashCommand("connect", "Connect your Discord account to your Steam account (verified via Steam login).")]
        public async Task Connect(InteractionContext context)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var existingSteamIdForUser = _store.GetSteamId64(context.User.Id);
            if (existingSteamIdForUser != null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"You already linked SteamID64 `{existingSteamIdForUser}`."));
                await Task.Delay(5000);
                await context.DeleteResponseAsync();
                return;
            }

            var state = Guid.NewGuid().ToString("N");
            SteamOpenIdBridge.RegisterPending(state, context.User.Id);

            var link = $"{PublicUrl}/steam/start?state={state}";

            var btn = new DiscordLinkButtonComponent(link, "Sign in through Steam");

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Link Steam Account")
                .WithDescription("Click the button below to sign in via Steam. After you finish, you can close the browser tab.")
                .WithColor(DiscordColor.SpringGreen);

            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(embed)
                .AddComponents(btn));

            await Task.Delay(5000);

            await context.DeleteResponseAsync();

        }
    }
}
