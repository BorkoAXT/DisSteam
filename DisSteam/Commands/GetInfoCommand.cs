using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace DisSteam.Commands
{
    
    public sealed class GetInfoCommand : BaseCommandModule
    {
        [Command("getinfo"), Description("Gets a user's steam information.")]
        [RequirePermissions(Permissions.All), RequireGuild]
        public async Task KickAsync(CommandContext context, [RemainingText] string reason = "No reason")
        {
            try
            {
                await context.RespondAsync("This command is under development.");
            }
            catch (DiscordException)
            {

                
            }
        }
    }
}
