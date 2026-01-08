using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DisSteam
{
    public sealed class SteamHttpServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly HttpClient _http = new HttpClient();
        private readonly string _publicBaseUrl;              // https://xxxxx.trycloudflare.com
        private readonly Func<string, string, Task<bool>> _onLinked; 

        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        /// <param name="publicBaseUrl">
        /// Your public URL (Cloudflare): https://suggestion-photographs-comparisons-happens.trycloudflare.com
        /// </param>
        /// <param name="onLinked">
        /// Called after successful OpenID verification. Return true if you stored the link successfully.
        /// </param>
        /// <param name="listenPrefix">
        /// Local prefix the bot listens on. Default: http://localhost:5050/
        /// </param>
        public SteamHttpServer(
            string publicBaseUrl,
            Func<string, string, Task<bool>> onLinked,
            string listenPrefix = "http://localhost:5050/")
        {
            _publicBaseUrl = publicBaseUrl.TrimEnd('/');
            _onLinked = onLinked;

            // HttpListener requires a trailing slash:
            if (!listenPrefix.EndsWith("/")) listenPrefix += "/";
            _listener.Prefixes.Add(listenPrefix);
        }

        public void Start()
        {
            if (_cts != null) throw new InvalidOperationException("Server already started.");

            _cts = new CancellationTokenSource();
            _listener.Start();
            _loopTask = Task.Run(() => ListenLoop(_cts.Token));
        }

        public async Task StopAsync()
        {
            if (_cts == null) return;

            _cts.Cancel();
            _listener.Stop();

            if (_loopTask != null)
                await _loopTask.ConfigureAwait(false);

            _cts.Dispose();
            _cts = null;
        }

        private async Task ListenLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener.IsListening)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch
                {
                    break; 
                }

                _ = Task.Run(() => Handle(ctx), ct);
            }
        }

        private async Task Handle(HttpListenerContext ctx)
        {
            try
            {
                var path = ctx.Request.Url?.AbsolutePath ?? "/";
                if (path.Equals("/steam/start", StringComparison.OrdinalIgnoreCase))
                {
                    await StartSteam(ctx).ConfigureAwait(false);
                    return;
                }

                if (path.Equals("/steam/return", StringComparison.OrdinalIgnoreCase))
                {
                    await FinishSteam(ctx).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 404;
                await WriteText(ctx, "Not found.").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try
                {
                    ctx.Response.StatusCode = 500;
                    await WriteText(ctx, $"Server error: {ex.Message}").ConfigureAwait(false);
                }
                catch { /* ignore */ }
            }
            finally
            {
                try { ctx.Response.OutputStream.Close(); } catch { }
                try { ctx.Response.Close(); } catch { }
            }
        }

        private Task StartSteam(HttpListenerContext ctx)
        {
            var q = ParseQuery(ctx.Request.Url);

            if (!q.TryGetValue("state", out var state) || string.IsNullOrWhiteSpace(state))
            {
                ctx.Response.StatusCode = 400;
                return WriteText(ctx, "Missing state.");
            }

            // include state in return_to so /steam/return knows it
            var returnTo = $"{_publicBaseUrl}/steam/return?state={Uri.EscapeDataString(state)}";

            var steamLogin =
                "https://steamcommunity.com/openid/login" +
                "?openid.ns=" + Uri.EscapeDataString("http://specs.openid.net/auth/2.0") +
                "&openid.mode=checkid_setup" +
                "&openid.return_to=" + Uri.EscapeDataString(returnTo) +
                "&openid.realm=" + Uri.EscapeDataString(_publicBaseUrl) +
                "&openid.identity=" + Uri.EscapeDataString("http://specs.openid.net/auth/2.0/identifier_select") +
                "&openid.claimed_id=" + Uri.EscapeDataString("http://specs.openid.net/auth/2.0/identifier_select");

            ctx.Response.StatusCode = 302;
            ctx.Response.RedirectLocation = steamLogin;
            return Task.CompletedTask;
        }


        private async Task FinishSteam(HttpListenerContext ctx)
        {
            var q = ParseQuery(ctx.Request.Url);

            if (!q.TryGetValue("state", out var state) || string.IsNullOrWhiteSpace(state))
            {
                ctx.Response.StatusCode = 400;
                await WriteText(ctx, "Missing state.").ConfigureAwait(false);
                return;
            }
            if (q.TryGetValue("openid.mode", out var mode) &&
                mode.Equals("cancel", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Response.StatusCode = 200;
                await WriteText(ctx, "You cancelled Steam sign-in. You can close this tab.").ConfigureAwait(false);
                return;
            }

            var valid = await VerifyOpenIdResponse(q).ConfigureAwait(false);
            if (!valid)
            {
                ctx.Response.StatusCode = 401;
                await WriteText(ctx, "Steam verification failed.").ConfigureAwait(false);
                return;
            }

            if (!q.TryGetValue("openid.claimed_id", out var claimed) || string.IsNullOrWhiteSpace(claimed))
            {
                ctx.Response.StatusCode = 400;
                await WriteText(ctx, "Missing openid.claimed_id.").ConfigureAwait(false);
                return;
            }

            var steamId64 = claimed.TrimEnd('/').Split('/').Last();
            if (!ulong.TryParse(steamId64, out _))
            {
                ctx.Response.StatusCode = 400;
                await WriteText(ctx, $"Invalid SteamID in claimed_id: {steamId64}").ConfigureAwait(false);
                return;
            }

            var stored = await _onLinked(state, steamId64).ConfigureAwait(false);
            if (!stored)
            {
                ctx.Response.StatusCode = 409;
                await WriteText(ctx, "Linking failed (expired state or already linked).").ConfigureAwait(false);
                return;
            }

            ctx.Response.StatusCode = 200;
            await WriteText(ctx, $"Linked successfully!\nSteamID64: {steamId64}\nYou can close this tab.").ConfigureAwait(false);
        }
        private async Task<bool> VerifyOpenIdResponse(Dictionary<string, string> q)
        {
            var data = q
                .Where(kv => kv.Key.StartsWith("openid.", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            data["openid.mode"] = "check_authentication";

            using var content = new FormUrlEncodedContent(data);
            using var resp = await _http.PostAsync("https://steamcommunity.com/openid/login", content).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            return body.Contains("is_valid:true", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task WriteText(HttpListenerContext ctx, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            ctx.Response.ContentType = "text/plain; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }
        private static Dictionary<string, string> ParseQuery(Uri? url)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (url == null) return dict;

            var query = url.Query;
            if (string.IsNullOrWhiteSpace(query)) return dict;

            if (query.StartsWith("?")) query = query[1..];
            var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var idx = part.IndexOf('=');
                if (idx <= 0) continue;

                var key = Uri.UnescapeDataString(part[..idx]);
                var val = Uri.UnescapeDataString(part[(idx + 1)..]);
                dict[key] = val;
            }

            return dict;
        }

        public void Dispose()
        {
            try { _listener.Stop(); } catch { }
            try { _listener.Close(); } catch { }
            _http.Dispose();
            _cts?.Dispose();
        }
    }
}
