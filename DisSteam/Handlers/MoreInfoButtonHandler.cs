using DisSteam.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RestSharp;
using UserStatus = Steam.Models.SteamCommunity.UserStatus;
using DisSteam.Views;

namespace DisSteam.Handlers
{
    public static class MoreInfoButtonHandler
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

        private static async Task OnComponentInteractionCreated(DiscordClient client,
            ComponentInteractionCreateEventArgs e)
        {
            if (e.Id.StartsWith("find:return:"))
            {
                var userId = ulong.Parse(e.Id.Split(':')[2]);
                var targetUser = await client.GetUserAsync(userId);

                await e.Interaction.CreateResponseAsync(
                    InteractionResponseType.UpdateMessage,
                    FindView.Build(e.User, targetUser));

                return;
            }

            var parts = e.Id.Split(":");
            ulong.TryParse(parts[2], out var discordUserId);

             _interface = _factory.CreateSteamWebInterface<SteamUser>(new HttpClient());
             ulong steamId = ulong.Parse(_store.GetSteamId64(discordUserId));
             var summary = await _interface.GetPlayerSummaryAsync(steamId);
             var data = summary.Data;
             var games = await _interface.GetCommunityProfileAsync(steamId);
             var friends = await _interface.GetFriendsListAsync(steamId);

            RestRequest levelRequest =
                 new RestRequest("https://api.steampowered.com/IPlayerService/GetSteamLevel/v1/", Method.Get)
                     .AddQueryParameter("key", _steamKey)
                     .AddQueryParameter("steamid", steamId);
             var response = await _client.ExecuteAsync(levelRequest);
             JsonDocument document = JsonDocument.Parse(response.Content);
             int level = document.RootElement
                 .GetProperty("response")
                 .GetProperty("player_level")
                 .GetInt32();
             document.Dispose();

             RestRequest gamesRequest =
                 new RestRequest("https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/", Method.Get)
                     .AddQueryParameter("key", _steamKey)
                     .AddQueryParameter("steamid", steamId)
                     .AddQueryParameter("include_played_free_games", true);

             var resp = await _client.ExecuteAsync(gamesRequest);
             JsonDocument gamesDocument = JsonDocument.Parse(resp.Content);
             int gameCount = gamesDocument.RootElement
                 .GetProperty("response")
                 .GetProperty("game_count")
                 .GetInt32();


            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Summary of player {_store.GetLink(discordUserId).PersonaName}")
                .WithImageUrl(data.AvatarUrl)
                .AddField("Status", data.UserStatus.ToString())
                .AddField(
                    "Currently playing",
                    data.UserStatus == UserStatus.Online && !string.IsNullOrWhiteSpace(data.PlayingGameName)
                        ? data.PlayingGameName
                        : "None"
                )
                .AddField("Location: ", data?.CountryCode)
                .AddField("Games owned: ", gameCount.ToString())
                .AddField("Friends count: ", friends.Data.Count.ToString())
                .AddField("Level: ", level.ToString());

            var backToInfoButton = new DiscordButtonComponent(
                ButtonStyle.Primary,
                customId: $"find:return:{discordUserId}",
                label: "Return To Info",
                emoji: new DiscordComponentEmoji("🔙"));

            var allGamesButton = new DiscordButtonComponent(
                ButtonStyle.Secondary,
                customId: $"allgames:{discordUserId}",
                label: "All Games",
                emoji: new DiscordComponentEmoji("🎮"));

            var allAchievementsButton = new DiscordButtonComponent(
                ButtonStyle.Secondary,
                customId: $"allachievements:{discordUserId}",
                label: "All Achievements",
                emoji: new DiscordComponentEmoji("🏆"));

            var allFriendsButton = new DiscordButtonComponent(
                ButtonStyle.Secondary,
                customId: $"allfriends:{discordUserId}",
                label: "All Friends",
                emoji: new DiscordComponentEmoji("👥"));

            var allBadgesButton = new DiscordButtonComponent(
                ButtonStyle.Secondary,
                customId: $"allbadges:{discordUserId}",
                label: "All Badges",
                emoji: new DiscordComponentEmoji("🎖️"));


            await e.Interaction.CreateResponseAsync(
                InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder().AddEmbed(embed)
                .AddComponents(backToInfoButton, allGamesButton, allAchievementsButton, allFriendsButton, allBadgesButton));
        }
    }
}
