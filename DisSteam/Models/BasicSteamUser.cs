namespace DisSteam.Models
{
    public class BasicSteamUser
    {
        public string? SteamId64 { get; set; } = string.Empty;
        public string? PersonaName { get; set; } = string.Empty;
        public string? ProfileUrl { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; } = string.Empty;

        public BasicSteamUser(string steamid, string personaname, string profileurl, string avatarurl)
        {
            SteamId64 = steamid;
            PersonaName = personaname;
            ProfileUrl = profileurl;
            AvatarUrl = avatarurl;
        }
    }
}
