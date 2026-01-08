using System.Net;
using System.Text;
using System.Web;

public class SteamHttpServer
{
    private readonly HttpListener _listener = new();
    private readonly string _publicBaseUrl;

    // state -> discordId
    private readonly Dictionary<string, ulong> _pending = new();

    // discordId -> steamId
    public readonly Dictionary<ulong, string> Links = new();

    public SteamHttpServer(string publicBaseUrl)
    {
        _publicBaseUrl = publicBaseUrl.TrimEnd('/');
        _listener.Prefixes.Add("http://localhost:5050/");
    }

    public void Start()
    {
        _listener.Start();
        _ = Task.Run(ListenLoop);
    }

    private async Task ListenLoop()
    {
        while (_listener.IsListening)
        {
            var ctx = await _listener.GetContextAsync();
            _ = Task.Run(() => Handle(ctx));
        }
    }

    private async Task Handle(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url!.AbsolutePath;

        if (path == "/steam/start")
            await StartSteam(ctx);
        else if (path == "/steam/return")
            await FinishSteam(ctx);
        else
            ctx.Response.StatusCode = 404;

        ctx.Response.Close();
    }
    private async Task StartSteam(HttpListenerContext ctx)
    {
        var query = HttpUtility.ParseQueryString(ctx.Request.Url!.Query);
        var state = query["state"];
        var discordId = query["discordId"];

        if (state == null || discordId == null)
        {
            ctx.Response.StatusCode = 400;
            return;
        }

        _pending[state] = ulong.Parse(discordId);

        var returnUrl = $"{_publicBaseUrl}/steam/return";

        var steamLogin =
            "https://steamcommunity.com/openid/login" +
            "?openid.ns=http://specs.openid.net/auth/2.0" +
            "&openid.mode=checkid_setup" +
            "&openid.return_to=" + Uri.EscapeDataString(returnUrl) +
            "&openid.realm=" + Uri.EscapeDataString(_publicBaseUrl) +
            "&openid.identity=http://specs.openid.net/auth/2.0/identifier_select" +
            "&openid.claimed_id=http://specs.openid.net/auth/2.0/identifier_select";

        ctx.Response.StatusCode = 302;
        ctx.Response.RedirectLocation = steamLogin;
        await Task.CompletedTask;
    }

    // STEP 2: Steam redirects back here
    private async Task FinishSteam(HttpListenerContext ctx)
    {
        var query = HttpUtility.ParseQueryString(ctx.Request.Url!.Query);
        var state = query["state"];

        if (state == null || !_pending.TryGetValue(state, out var discordId))
        {
            ctx.Response.StatusCode = 400;
            return;
        }

        if (!await VerifySteamResponse(query))
        {
            ctx.Response.StatusCode = 401;
            return;
        }

        var claimed = query["openid.claimed_id"];
        var steamId = claimed!.Split('/').Last();

        Links[discordId] = steamId;
        _pending.Remove(state);

        await Write(ctx, $"Linked successfully! SteamID: {steamId}\nYou can close this tab.");
    }

    private async Task<bool> VerifySteamResponse(System.Collections.Specialized.NameValueCollection q)
    {
        var data = new Dictionary<string, string>();
        foreach (var key in q.AllKeys!)
        {
            if (key!.StartsWith("openid."))
                data[key] = q[key]!;
        }
        data["openid.mode"] = "check_authentication";

        using var client = new HttpClient();
        var res = await client.PostAsync(
            "https://steamcommunity.com/openid/login",
            new FormUrlEncodedContent(data)
        );

        var body = await res.Content.ReadAsStringAsync();
        return body.Contains("is_valid:true");
    }

    private async Task Write(HttpListenerContext ctx, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        ctx.Response.ContentType = "text/plain";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes);
    }
}
