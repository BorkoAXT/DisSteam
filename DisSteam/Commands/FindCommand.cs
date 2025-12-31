using DisSteam.Data;
using DisSteam.Views;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace DisSteam.Commands
{
    public sealed class FindCommand : ApplicationCommandModule
    {
        private static readonly LinkStore _store = new LinkStore("links.db");

        [SlashCommand("find", "Finds the Steam profile of a user")]
        public async Task Find(InteractionContext context, [Option("user", "The user")] DiscordUser user)
        {
            await context.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource);

            var steamId = _store.GetSteamId64(user.Id);
            if (steamId == null)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder()
                        .WithContent("This user hasn't linked their Discord account with Steam."));

                await Task.Delay(7000);
                await context.DeleteResponseAsync();

                return;
            }

            await context.EditResponseAsync(
                new DiscordWebhookBuilder(
                FindView.Build(context.User, user)));
        }
    }
}
