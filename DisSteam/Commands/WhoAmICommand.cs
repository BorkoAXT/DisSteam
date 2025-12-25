using DisSteam.Data;
using DisSteam.Views;
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
                InteractionResponseType.DeferredChannelMessageWithSource);

            var link = _store.GetLink(context.User.Id);
            if (link == null)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder()
                        .WithContent("You have not linked a Steam account yet."));

                await Task.Delay(5000);
                await context.DeleteResponseAsync();

                return;
            }

            await context.EditResponseAsync(
                new DiscordWebhookBuilder(
                    FindView.Build(context.User, context.User)));
        }
    }
}