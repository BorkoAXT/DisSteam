using DisSteam.Data;
using DisSteam.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using RestSharp;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Text.Json;

namespace DisSteam.Handlers
{
    public static class AllGamesHandler
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
            if (!e.Id.StartsWith("allgames:"))
            {
                return;
            }

            var idParts = e.Id.Split(":");

            ulong.TryParse(idParts[1], out ulong discordUserId);

            int page = int.Parse(idParts[2]);

            _interface = _factory.CreateSteamWebInterface<SteamUser>(new HttpClient());
            ulong steamId = ulong.Parse(_store.GetSteamId64(discordUserId));

            var games = await _interface.GetCommunityProfileAsync(steamId);

            RestRequest gamesRequest =
                 new RestRequest("https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/", Method.Get)
                     .AddQueryParameter("key", _steamKey)
                     .AddQueryParameter("steamid", steamId)
                     .AddQueryParameter("include_played_free_games", true);

            var gamesResponse = await _client.ExecuteAsync(gamesRequest);
            JsonDocument gamesDocument = JsonDocument.Parse(gamesResponse.Content);

            List<Game> gamesList = gamesDocument.RootElement.GetProperty("response").GetProperty("games").Deserialize<List<Game>>().ToList();

            gamesList = gamesList
                .OrderByDescending(g => g.PlayTime)
                .Skip((page - 1) * 3)
                .Take(3)
                .ToList();

            int gameCount = gamesDocument.RootElement
                .GetProperty("response")
                .GetProperty("game_count")
                .GetInt32();

            int pageSize = 3;
            int totalPages = (int)Math.Ceiling(gameCount / (double)pageSize);

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"{_store.GetLink(discordUserId).PersonaName}'s Games:")
                .WithFooter($"Page {page} / {totalPages}");

            foreach (var game in gamesList)
            {
                // Get Game Info
                RestRequest gameInfoRequest = new RestRequest("https://store.steampowered.com/api/appdetails", Method.Get)
                    .AddQueryParameter("appids", game.AppId.ToString());

                var gameInfoResponse = await _client.ExecuteAsync(gameInfoRequest);

                var gameInfoJson = gameInfoResponse.Content;

                JsonDocument gameInfoDocument = JsonDocument.Parse(gameInfoJson);

                var gameData = gameInfoDocument.RootElement.GetProperty(game.AppId.ToString()).GetProperty("data");

                string gameName = gameData.GetProperty("name").ToString();

                int playtimeHours = game.PlayTime / 60;

                // Get Achievements
                RestRequest userGameAchievementsRequest = new RestRequest("https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v1/", Method.Get)
                    .AddQueryParameter("key", _steamKey)
                    .AddQueryParameter("steamId", steamId)
                    .AddQueryParameter("appid", game.AppId);

                var userGameAchievementsResponse = await _client.ExecuteAsync(userGameAchievementsRequest);

                if (userGameAchievementsResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    int totalAchievements = 0;
                    int unlockedAchievements = 0;

                    var userGameAchievementsJson = userGameAchievementsResponse.Content;
                    JsonDocument achievementsDocument = JsonDocument.Parse(userGameAchievementsJson);

                    var playerStats = achievementsDocument.RootElement.GetProperty("playerstats");

                    if (playerStats.TryGetProperty("achievements", out JsonElement achievements))
                    {
                        totalAchievements = achievements.GetArrayLength();
                        foreach (var ach in achievements.EnumerateArray())
                        {
                            if (ach.GetProperty("achieved").GetInt32() == 1)
                            {
                                unlockedAchievements++;
                            }
                        }
                    }
                    embed.AddField(
                        $"{gameName}",
                        $"Playtime: {playtimeHours} hours\nAchievements: {unlockedAchievements}/{totalAchievements}",
                        false
                    );
                }
                else
                {
                    embed.AddField(
                        $"{gameName}",
                        $"Playtime: {playtimeHours} hours\nAchievements: N/A",
                        false
                    );
                }
            }

            var components = new List<DiscordComponent>();

            // Return button (always visible)
            components.Add(new DiscordButtonComponent(
                ButtonStyle.Primary,
                customId: $"find:info:{discordUserId}",
                label: "Return To Info",
                emoji: new DiscordComponentEmoji("🔙")
            ));

            // Previous page button (disabled if on first page)
            components.Add(new DiscordButtonComponent(
                ButtonStyle.Secondary,
                customId: $"allgames:{discordUserId}:{page - 1}",
                label: "Previous",
                emoji: new DiscordComponentEmoji("◀"),
                disabled: page <= 1
            ));

            // Next page button (disabled if on last page)
            components.Add(new DiscordButtonComponent(
                ButtonStyle.Secondary,
                customId: $"allgames:{discordUserId}:{page + 1}",
                label: "Next",
                emoji: new DiscordComponentEmoji("▶"),
                disabled: page >= totalPages
            ));

            await e.Interaction.CreateResponseAsync(
                InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed)
                    .AddComponents(components)
            );
        }
    }
}
