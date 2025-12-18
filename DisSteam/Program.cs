using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using RestSharp;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DisSteam
{
    internal class Program
    {

        private static CancellationTokenSource _cts { get; set; }

        private static DiscordConfiguration _config { get; set; }

        private static DiscordClient _client;

        private static CommandsNextConfiguration _cnc;

        private static CommandsNextExtension _cne;

        private static Token _token;

        private static DiscordActivity _activity;

        private static InteractionType _interactionType;

        private static RestClient _restClient;


        public static async Task Main(string[] args)
        {
            
            string? json = File.ReadAllText("C:\\Users\\borko\\source\\repos\\New folder\\DisSteam\\DisSteam\\config.json");
            _token = JsonSerializer.Deserialize<Token>(json);
            
            
            _cts = new CancellationTokenSource();

            _config = new DiscordConfiguration()
            {
                AutoReconnect = true,
                TokenType = TokenType.Bot,
                Token = _token.token,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
            };
            _client = new DiscordClient(_config);

            _activity = new DiscordActivity("with Steam APIs!", ActivityType.Playing);

            _cnc = new CommandsNextConfiguration() { StringPrefixes = new[] { "/" } };
            _cne = _client.UseCommandsNext(_cnc);

            _cne.RegisterCommands(typeof(Program).Assembly);

            await _client.ConnectAsync(_activity, UserStatus.Online);

            await Task.Delay(-1, _cts.Token);


        }
    }
}