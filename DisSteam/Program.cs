using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using RestSharp;
using DisSteam.Commands;
using DisSteam.Handlers;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DisSteam
{
    internal class Program
    {

        private static CancellationTokenSource _cts { get; set; }

        private static DiscordConfiguration _config { get; set; }

        private static DiscordClient _client;

        private static string? _token;

        private static ulong guidId = 1458750358930063455;
        public static Dictionary<string, string> Connects { get; set; }

        private static DiscordActivity _activity;

        private static SteamHttpServer _steamServer;

        private static InteractionType _interactionType;

        private static RestClient _restClient;


        public static async Task Main(string[] args)
        {

            _token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
            
            _cts = new CancellationTokenSource();

            _config = new DiscordConfiguration()
            {
                AutoReconnect = true,
                TokenType = TokenType.Bot,
                Token = _token,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.Guilds
            };
            _client = new DiscordClient(_config);

            _activity = new DiscordActivity("With Steam's API!", ActivityType.Playing);

            var slash = _client.UseSlashCommands();

            slash.RegisterCommands<ConnectCommand>(guidId);
            slash.RegisterCommands<WhoAmICommand>(guidId);
            slash.RegisterCommands<UnlinkCommand>(guidId);
            slash.RegisterCommands<FindCommand>(guidId);
            UnlinkButtonHandler.Register(_client);
            MoreInfoButtonHandler.Register(_client);
            ReturnButtonHandler.Register(_client);
            AllGamesHandler.Register(_client);
            AllFriendsHandler.Register(_client);
            AllBadgesHandler.Register(_client);

            var publicUrl = "https://suggestion-photographs-comparisons-happens.trycloudflare.com";

            var steamServer = new SteamHttpServer(
                publicUrl,
                async (state, steamId64) =>
                {
                    return await SteamOpenIdBridge.CompleteAsync(state, steamId64);
                });

            steamServer.Start();

            await _client.ConnectAsync(_activity, UserStatus.Online);

            await Task.Delay(-1, _cts.Token);
        }
    }
}