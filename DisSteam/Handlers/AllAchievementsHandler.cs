using DSharpPlus;
using DSharpPlus.EventArgs;

namespace DisSteam.Handlers
{
    public static class AllAchievementsHandler
    {
        public static void Register(DiscordClient client)
        {
            client.ComponentInteractionCreated += OnComponentInteractionCreated;
        }
        private static async Task OnComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
