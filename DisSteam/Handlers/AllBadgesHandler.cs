using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HtmlAgilityPack;
using RestSharp;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Text.Json;
using DisSteam.Data;
using DisSteam.Models;

namespace DisSteam.Handlers
{
    public static class AllBadgesHandler
    {
        private static readonly LinkStore _store = new LinkStore("links.db");
        private static readonly string? _steamKey = Environment.GetEnvironmentVariable("STEAM_API_KEY");
        private static SteamUser _interface;
        private static RestClient _client = new RestClient();

        private static readonly SteamWebInterfaceFactory _factory =
            new SteamWebInterfaceFactory(_steamKey);

        public static void Register(DiscordClient client)
        {
            client.ComponentInteractionCreated += OnComponentInteractionCreated;
        }

        private static async Task OnComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs e)
        {
            if (!e.Id.StartsWith("allbadges:"))
                return;

            var idParts = e.Id.Split(":");
            ulong.TryParse(idParts[1], out ulong discordUserId);
            int page = int.Parse(idParts[2]);

            _interface = _factory.CreateSteamWebInterface<SteamUser>(new HttpClient());
            ulong steamId = ulong.Parse(_store.GetSteamId64(discordUserId));

            // Fetch badges from Steam API
            RestRequest badgesRequest = new RestRequest("https://api.steampowered.com/IPlayerService/GetBadges/v1/", Method.Get)
                .AddQueryParameter("key", _steamKey)
                .AddQueryParameter("steamid", steamId);

            var badgesResponse = await _client.ExecuteAsync(badgesRequest);
            JsonDocument badgesDocument = JsonDocument.Parse(badgesResponse.Content);

            // Deserialize and order badges
            var allBadges = badgesDocument.RootElement
                .GetProperty("response")
                .GetProperty("badges")
                .Deserialize<List<Badge>>()
                .OrderBy(b => b.BadgeId)
                .ThenBy(b => b.AppId ?? 0)
                .ToList();

            int pageSize = 5;
            int totalPages = (int)Math.Ceiling(allBadges.Count / (double)pageSize);
            page = Math.Clamp(page, 1, totalPages);

            var badgesList = allBadges
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 1️⃣ Create placeholder embed immediately (fast)
            var placeholderEmbed = new DiscordEmbedBuilder()
                .WithTitle($"{_store.GetLink(discordUserId).PersonaName}'s Badges:")
                .WithDescription("Loading badge names...");

            var placeholderComponents = new List<DiscordComponent>()
            {
                new DiscordButtonComponent(
                    ButtonStyle.Primary,
                customId: $"find:info:{discordUserId}",
                label: "Return To Info",
                emoji: new DiscordComponentEmoji("🔙")),

                new DiscordButtonComponent(
                    ButtonStyle.Secondary, 
                    customId: $"allbadges:{discordUserId}:{page - 1}", 
                    label: "Previous",
                    emoji: new DiscordComponentEmoji("◀"), disabled: page <= 1),

                new DiscordButtonComponent(
                    ButtonStyle.Secondary, 
                    customId: $"allbadges:{discordUserId}:{page + 1}", 
                    label: "Next", 
                    emoji: new DiscordComponentEmoji("▶"), 
                    disabled: page >= totalPages)
            };

            // Respond immediately with placeholder to satisfy Discord
            await e.Interaction.CreateResponseAsync(
                InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(placeholderEmbed)
                    .AddComponents(placeholderComponents)
            );

            // 2️⃣ Scrape badge pages asynchronously
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"{_store.GetLink(discordUserId).PersonaName}'s Badges:");

            foreach (var badge in badgesList)
            {
                string badgeUrl;
                if (badge.AppId.HasValue)
                {
                    badgeUrl = $"https://steamcommunity.com/profiles/{steamId}/badges/{badge.AppId.Value}";
                    if (badge.BorderColor == 1)
                        badgeUrl += "?border=1";
                }
                else
                {
                    badgeUrl = $"https://steamcommunity.com/profiles/{steamId}/badges/{badge.BadgeId}";
                }

                var request = new RestRequest(badgeUrl, Method.Get);
                request.AddHeader("User-Agent", "Mozilla/5.0");

                var response = await _client.ExecuteAsync(request);
                string html = response.Content;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var titleNode = doc.DocumentNode
                    .SelectSingleNode("//div[contains(@class,'badge_info_title')]");

                string badgeName = titleNode?.InnerText.Trim()
                    ?? $"Unknown Badge ({badge.BadgeId})";

                embed.AddField(
                    badgeName,
                    $"Level: {badge.Level}\nXP: {badge.Xp}",
                    false);

                await Task.Delay(300);
            }

            // 3️⃣ Edit the original message to fill in badge names
            await e.Interaction.EditOriginalResponseAsync(
                new DiscordWebhookBuilder()
                    .AddEmbed(embed)
                    .AddComponents(placeholderComponents) // same components
            );
        }
    }
}
