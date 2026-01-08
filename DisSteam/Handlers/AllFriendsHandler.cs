using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using RestSharp;
using System.Text.Json;
using DisSteam.Data;

namespace DisSteam.Handlers
{
    public static class AllFriendsHandler
    {
        private static readonly LinkStore _store = new LinkStore("links.db");
        private static readonly string? _steamKey = Environment.GetEnvironmentVariable("STEAM_API_KEY");
        private static readonly RestClient _client = new RestClient();

        public static void Register(DiscordClient client)
        {
            client.ComponentInteractionCreated += OnComponentInteractionCreated;
        }

        private static async Task OnComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs e)
        {
            if (!e.Id.StartsWith("allfriends:"))
                return;

            // allfriends:{targetDiscordId}:{page}
            var parts = e.Id.Split(":");
            ulong.TryParse(parts[1], out ulong targetDiscordId);
            int page = int.Parse(parts[2]);

            var targetSteamIdStr = _store.GetSteamId64(targetDiscordId);
            if (string.IsNullOrWhiteSpace(targetSteamIdStr))
            {
                await e.Interaction.CreateResponseAsync(
                    InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().WithContent("User is not linked to Steam.")
                );
                return;
            }

            ulong targetSteamId = ulong.Parse(targetSteamIdStr);

            // --- Get target friends ---
            var targetReq = new RestRequest("https://api.steampowered.com/ISteamUser/GetFriendList/v1/", Method.Get)
                .AddQueryParameter("key", _steamKey)
                .AddQueryParameter("steamid", targetSteamId)
                .AddQueryParameter("relationship", "friend");

            var targetResp = await _client.ExecuteAsync(targetReq);
            if (!targetResp.IsSuccessful || string.IsNullOrWhiteSpace(targetResp.Content))
            {
                await e.Interaction.CreateResponseAsync(
                    InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().WithContent("Friends list is private or unavailable.")
                );
                return;
            }

            JsonDocument targetDoc;
            try
            {
                targetDoc = JsonDocument.Parse(targetResp.Content);
            }
            catch
            {
                await e.Interaction.CreateResponseAsync(
                    InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().WithContent("Steam response parse error.")
                );
                return;
            }

            if (!targetDoc.RootElement.TryGetProperty("friendslist", out var targetFriendsListEl) ||
                !targetFriendsListEl.TryGetProperty("friends", out var targetFriendsEl))
            {
                await e.Interaction.CreateResponseAsync(
                    InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().WithContent("Friends list is private or unavailable.")
                );
                return;
            }

            var targetFriends = targetFriendsEl
                .EnumerateArray()
                .Select(x => x.TryGetProperty("steamid", out var sid) ? sid.GetString() : null)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            // --- Optional: Get viewer friends for mutuals ---
            var mutual = new List<string>();
            var viewerSteamIdStr = _store.GetSteamId64(e.User.Id);

            if (!string.IsNullOrWhiteSpace(viewerSteamIdStr))
            {
                var viewerReq = new RestRequest("https://api.steampowered.com/ISteamUser/GetFriendList/v1/", Method.Get)
                    .AddQueryParameter("key", _steamKey)
                    .AddQueryParameter("steamid", viewerSteamIdStr)
                    .AddQueryParameter("relationship", "friend");

                var viewerResp = await _client.ExecuteAsync(viewerReq);
                if (viewerResp.IsSuccessful && !string.IsNullOrWhiteSpace(viewerResp.Content))
                {
                    try
                    {
                        var viewerDoc = JsonDocument.Parse(viewerResp.Content);

                        if (viewerDoc.RootElement.TryGetProperty("friendslist", out var viewerFriendsListEl) &&
                            viewerFriendsListEl.TryGetProperty("friends", out var viewerFriendsEl))
                        {
                            var viewerSet = viewerFriendsEl
                                .EnumerateArray()
                                .Select(x => x.TryGetProperty("steamid", out var sid) ? sid.GetString() : null)
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .ToHashSet();

                            mutual = targetFriends.Where(x => viewerSet.Contains(x)).ToList();
                        }
                    }
                    catch
                    {
                        // ignore mutuals if viewer list can't be parsed (keep it simple)
                    }
                }
            }

            var others = targetFriends.Where(x => !mutual.Contains(x)).ToList();

            // Mutuals first, then others
            var ordered = mutual.Concat(others).ToList();

            int pageSize = 10;
            int totalPages = Math.Max(1, (int)Math.Ceiling(ordered.Count / (double)pageSize));
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var pageFriends = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var persona = _store.GetLink(targetDiscordId)?.PersonaName ?? "User";

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"{persona}'s Friends")
                .WithFooter($"Page {page} / {totalPages}");

            if (pageFriends.Count == 0)
            {
                embed.WithDescription("No friends found.");
            }
            else
            {
                int index = (page - 1) * pageSize + 1;
                foreach (var sid in pageFriends)
                {
                    bool isMutual = mutual.Contains(sid);
                    embed.AddField(
                        $"{index++}. {(isMutual ? "⭐ Mutual" : "Friend")}",
                        sid!,
                        false
                    );
                }
            }

            var returnBtn = new DiscordButtonComponent(
                ButtonStyle.Primary,
                customId: $"find:info:{targetDiscordId}",
                label: "Return To Info",
                emoji: new DiscordComponentEmoji("🔙")
            );

            var prevBtn = new DiscordButtonComponent(
                ButtonStyle.Secondary,
                customId: $"allfriends:{targetDiscordId}:{page - 1}",
                label: "Previous",
                emoji: new DiscordComponentEmoji("◀"),
                disabled: page <= 1
            );

            var nextBtn = new DiscordButtonComponent(
                ButtonStyle.Secondary,
                customId: $"allfriends:{targetDiscordId}:{page + 1}",
                label: "Next",
                emoji: new DiscordComponentEmoji("▶"),
                disabled: page >= totalPages
            );

            await e.Interaction.CreateResponseAsync(
                InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed)
                    .AddComponents(returnBtn, prevBtn, nextBtn)
            );
        }
    }
}
