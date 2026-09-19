using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using CompanySignage.Player.Models;
using CompanySignage.Player.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json.Linq;

class Program
{
    static int checks;
    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        checks++;
        Console.WriteLine("PASS: " + name);
    }

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            var state = JObject.Parse(File.ReadAllText(args[0]));
            Environment.SetEnvironmentVariable("COMPANY_SIGNAGE_DATA_DIR", args[1]);
            var settings = new PlayerSettings { ServerUrl=(string)state["baseUrl"],
                ScreenCode=(string)state["screenCode"], DeviceToken=(string)state["token"] };
            Task.Run(async () =>
            {
                var cache = new CacheService(settings);
                var manifest = await cache.FetchPublicationAsync();
                Check(manifest != null && manifest.PlaylistItems.Count == 2, "Player parses authenticated playlist");
                Check(await cache.DownloadContentAsync(manifest), "Player downloads and checks hashes");
                var path = cache.GetLocalPath(manifest.PlaylistItems[0].StoredFileName);
                var original = File.ReadAllBytes(path);
                File.WriteAllText(path, "corrupt cache");
                Check(await cache.DownloadContentAsync(manifest), "Corrupt cache is downloaded again");
                Check(Convert.ToBase64String(File.ReadAllBytes(path)) == Convert.ToBase64String(original), "Cache bytes restored");
                var offline = new CacheService(new PlayerSettings { ServerUrl="http://127.0.0.1:1", ScreenCode=settings.ScreenCode, DeviceToken=settings.DeviceToken });
                Check(await offline.FetchPublicationAsync() == null, "Offline fetch fails gracefully");
                Check(PlaylistManifest.Load()?.PlaylistItems.Count == 2 && File.Exists(path), "Offline publication and files survive");
                var invalid = new CacheService(new PlayerSettings { ServerUrl=settings.ServerUrl, ScreenCode=settings.ScreenCode, DeviceToken="invalid" });
                Check(await invalid.FetchPublicationAsync() == null, "Real player cannot use wrong token");
                var hub = new HubConnectionBuilder().WithUrl(settings.ServerUrl + "/signageHub", options =>
                {
                    options.Headers.Add("X-Screen-Code", settings.ScreenCode);
                    options.Headers.Add("X-Device-Token", settings.DeviceToken);
                }).Build();
                try
                {
                    await hub.StartAsync();
                    await hub.InvokeAsync("JoinScreenGroup", settings.ScreenCode);
                    Check(hub.State == HubConnectionState.Connected, "SignalR connects and joins own screen");
                    var rejected = false;
                    try { await hub.InvokeAsync("JoinScreenGroup", (string)state["otherScreen"]); }
                    catch (Microsoft.AspNetCore.SignalR.HubException) { rejected = true; }
                    Check(rejected, "SignalR rejects another screen group");
                }
                finally { await hub.DisposeAsync(); }
            }).GetAwaiter().GetResult();

            var grid = new Grid();
            using (var playback = new PlaybackService(grid, new CacheService(settings), settings))
            {
                var type = typeof(PlaybackService);
                var field = type.GetField("_manifest", BindingFlags.NonPublic | BindingFlags.Instance);
                var start = type.GetMethod("StartPlayback", BindingFlags.NonPublic | BindingFlags.Instance);
                grid.Children.Add(new TextBlock { Text="old publication" });
                field.SetValue(playback, new PlaylistManifest());
                start.Invoke(playback, null);
                Check(grid.Children.Count == 0, "Empty publication removes old display");
                var missing = new PlaylistManifest { AssignmentType="Playlist" };
                missing.PlaylistItems.Add(new ManifestItem { StoredFileName="missing-a.png" });
                missing.PlaylistItems.Add(new ManifestItem { StoredFileName="missing-b.png" });
                field.SetValue(playback, missing);
                start.Invoke(playback, null);
                Check(grid.Children.Count == 0, "Missing playlist files do not recurse forever");
            }
            Console.WriteLine(checks + " player checks passed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
}
