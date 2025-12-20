using System.Threading.Tasks;
using DisSteam.Data;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace DisSteam.Commands
{
    public sealed class WhoAmICommand : ApplicationCommandModule
    {
        private static readonly LinkStore _store = new LinkStore("links.db");

        [SlashCommand("whoami", "Shows you the steam profile you are linked to.")]
        public async Task WhoAmI(InteractionContext context)
        {
            await context.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            var link = _store.GetLink(context.User.Id);
            if (link == null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You have not linked a Steam account yet."));
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Linked Steam profile: {link.Value.PersonaName}")
                .WithUrl(string.IsNullOrWhiteSpace(link.Value.ProfileUrl) ? null : link.Value.ProfileUrl)
                .WithThumbnail(string.IsNullOrWhiteSpace(link.Value.AvatarUrl) ? null : link.Value.AvatarUrl)
                .AddField("SteamID64", link.Value.SteamId64, true)
                .WithFooter($"Requested by {context.User.Username}");

            await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}