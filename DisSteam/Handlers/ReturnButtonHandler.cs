using DSharpPlus;
using DSharpPlus.EventArgs;

namespace DisSteam.Handlers
{
    public static class ReturnButtonHandler
    {
        public static void Register(DiscordClient client)
        {
            client.ComponentInteractionCreated += OnComponentInteractionCreated;
        }

        private static async Task OnComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
