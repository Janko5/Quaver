using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu_database_reader.BinaryFiles;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics.Backgrounds;
using Quaver.Shared.Graphics;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Helpers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.Download;
using Quaver.Shared.Online.API.Maps;
using Wobble.Graphics.UI.Dialogs;
using Quaver.Shared.Online.API.Playlists;
using Quaver.Shared.Scheduling;
using Quaver.Server.Client.Events.Download;
using Quaver.Server.Client.Helpers;
using Quaver.Shared.Screens.Selection;
using Quaver.Shared.Screens.Importing;
using Quaver.Shared.Screens.Main;
using SQLite;
using Wobble;
using Wobble.Bindables;
using Wobble.Logging;

namespace Quaver.Shared.Database.Playlists
{
    public static class PlaylistManager
    {
        /// <summary>
        ///     The available playlists
        /// </summary>
        public static List<Playlist> Playlists { get; private set; } = new List<Playlist>();

        /// <summary>
        ///     The currently selected playlist
        /// </summary>
        public static Bindable<Playlist> Selected { get; } = new Bindable<Playlist>(null);

        /// <summary>
        ///     Event invoked when a new playlist has been created
        /// </summary>
        public static event EventHandler<PlaylistCreatedEventArgs> PlaylistCreated;

        /// <summary>
        ///     Event invoked when a playlist has been deleted from the game
        /// </summary>
        public static event EventHandler<PlaylistDeletedEventArgs> PlaylistDeleted;

        /// <summary>
        ///     Event invoked when a playlist has been synced to a map pool
        /// </summary>
        public static event EventHandler<PlaylistSyncedEventArgs> PlaylistSynced;

        /// <summary>
        ///     Event invoked when a playlist's maps have been managed
        /// </summary>
        public static event EventHandler<PlaylistMapsManagedEventArgs> PlaylistMapsManaged;

        /// <summary>
        ///     Loads all of the maps in the database and groups them into mapsets to use
        ///     for gameplay
        /// </summary>
        public static void Load()
        {
            CreateTables();
            LoadPlaylists();

            try
            {
                if (ConfigManager.AutoLoadOsuBeatmaps.Value)
                    LoadOsuCollections();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to load osu! collections!", LogType.Runtime);
                Logger.Error(e, LogType.Runtime);
            }

            try
            {
                // Remove all ett charts if the db isn't loaded
                if (ConfigManager.AutoLoadOsuBeatmaps.Value && !string.IsNullOrEmpty(ConfigManager.EtternaDbPath.Value))
                {
                    var charts = new List<Map>();
                    var playlists = new List<Playlist>();

                    foreach (var mapset in MapManager.Mapsets)
                    {
                        var found = mapset.Maps.FindAll(x => x.Game == MapGame.Etterna);
                        charts.AddRange(found);
                    }

                    var packs = charts.GroupBy(x => new DirectoryInfo(Path.GetFullPath(Path.Combine(x.Directory, @"..\"))).Name).ToDictionary(x => x.Key);

                    foreach (var item in packs)
                    {
                        var playlist = new Playlist()
                        {
                            Id = -(Playlists.Count + 1),
                            Name = item.Key,
                            Creator = "External Game",
                            Maps = new List<Map>(),
                            PlaylistGame = MapGame.Etterna
                        };

                        foreach (var map in item.Value)
                            playlist.Maps.Add(map);

                        playlists.Add(playlist);
                    }

                    playlists = playlists.OrderBy(x => x.Name).ToList();
                    Playlists.AddRange(playlists);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
            }
        }

        /// <summary>
        ///     Creates the `playlists` database table.
        /// </summary>
        private static void CreateTables()
        {
            try
            {
                DatabaseManager.Connection.CreateTable<Playlist>();
                Logger.Important($"Playlist Table has been created", LogType.Runtime);

                DatabaseManager.Connection.CreateTable<PlaylistMap>();
                Logger.Important($"PlaylistMap table has been created", LogType.Runtime);
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
                throw;
            }
        }

        /// <summary>
        ///     Loads playlists from the database
        /// </summary>
        private static void LoadPlaylists()
        {
            var tasks = new List<Task>();
            Playlists = new List<Playlist>();
            Selected.Value = null;

            try
            {
                var playlists = DatabaseManager.Connection.Table<Playlist>().ToList();
                var playlistMaps = DatabaseManager.Connection.Table<PlaylistMap>().ToList();

                var playlistDictionary = ConcurrentDictionaryExtensions.ToConcurrentDictionary(playlists, playlist => playlist.Id);

                foreach (var playlist in playlists)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        foreach (var mapset in MapManager.Mapsets)
                        {
                            foreach (var map in mapset.Maps)
                            {
                                var playlistMap = playlistMaps.Find(x =>
                                    (x.PlaylistId == playlist.Id) && (x.Md5 == map.Md5Checksum));

                                if (playlistMap != null)
                                    playlistDictionary[playlist.Id].Maps.Add(map);
                            }
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());

                Playlists = Playlists.Concat(playlists).OrderBy(x => x.Name).ToList();

                foreach (var playlist in playlists)
                    Logger.Important($"Loaded Quaver playlist: {playlist.Name ?? ""} w/ {playlist.Maps?.Count ?? 0} maps!", LogType.Runtime);
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
            }
        }

        /// <summary>
        ///    Loads osu! collections and converts them to Quaver playlists
        /// </summary>
        private static void LoadOsuCollections()
        {
            var path = ConfigManager.OsuDbPath.Value.Replace("osu!.db", "collection.db");

            if (!File.Exists(path))
                return;

            var db = CollectionDb.Read(path);

            var playlists = new List<Playlist>();

            for (var i = 0; i < db.Collections.Count; i++)
            {
                var collection = db.Collections[i];

                var playlist = new Playlist
                {
                    Id = -i,
                    Name = collection.Name,
                    Creator = "External Game",
                    Maps = new List<Map>(),
                    PlaylistGame = MapGame.Osu
                };

                foreach (var hash in collection.BeatmapHashes)
                {
                    foreach (var mapset in MapManager.Mapsets)
                    {
                        var map = mapset.Maps.Find(x => x.Md5Checksum == hash);

                        if (map == null)
                            continue;

                        playlist.Maps.Add(map);
                        break;
                    }
                }

                // Don't load empty osu! collections
                if (playlist.Maps.Count == 0)
                {
                    Logger.Important($"Skipping load on osu! playlist: {playlist.Name ?? ""} because it is empty!", LogType.Runtime);
                    continue;
                }

                playlists.Add(playlist);
            }

            Playlists = Playlists.Concat(playlists).ToList();

            foreach (var playlist in playlists)
                Logger.Important($"Loaded osu! playlist: {playlist.Name ?? ""} w/ {playlist.Maps?.Count ?? 0} maps!", LogType.Runtime);
        }

        /// <summary>
        ///     Adds a playlist to the database
        /// </summary>
        /// <param name="playlist"></param>
        public static int AddPlaylist(Playlist playlist, string bannerPath = null)
        {
            var id = -1;

            try
            {
                id = DatabaseManager.Connection.Insert(playlist);

                Logger.Important($"Successfully added playlist: {playlist.Name} (#{playlist.Id}) (by: {playlist.Creator}) to the database", LogType.Runtime);

                CopyPlaylistBanner(playlist, bannerPath);
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
            }
            finally
            {
                if (!Playlists.Contains(playlist))
                    Playlists.Add(playlist);

                Playlists = Playlists.OrderBy(x => x.PlaylistGame).ThenBy(x => x.Name).ToList();
                Selected.Value = playlist;

                PlaylistCreated?.Invoke(typeof(PlaylistManager), new PlaylistCreatedEventArgs(playlist));
            }

            return id;
        }

        /// <summary>
        /// </summary>
        /// <param name="playlist"></param>
        public static void CopyPlaylist(Playlist playlist)
        {
            var newPlaylist = new Playlist()
            {
                Name = $"{playlist.Name} (Copy)",
                Description = playlist.Description,
                Creator = playlist.Creator,
                OnlineMapPoolId = playlist.OnlineMapPoolId,
                OnlineMapPoolCreatorId = playlist.OnlineMapPoolCreatorId
            };

            playlist.Maps.ForEach(x => newPlaylist.Maps.Add(x));

            AddPlaylist(newPlaylist);
            ThreadScheduler.Run(() => newPlaylist.Maps.ForEach(x => AddMapToPlaylist(newPlaylist, x)));
        }

        /// <summary>
        /// </summary>
        public static void EditPlaylist(Playlist playlist, string bannerPath, bool emitEvent = true)
        {
            try
            {
                DatabaseManager.Connection.Update(playlist);
                CopyPlaylistBanner(playlist, bannerPath);
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
            }

            if (emitEvent)
                PlaylistCreated?.Invoke(typeof(PlaylistManager), new PlaylistCreatedEventArgs(playlist));
        }

        /// <summary>
        ///     Deletes a playlist from the database
        /// </summary>
        /// <param name="playlist"></param>
        public static void DeletePlaylist(Playlist playlist)
        {
            try
            {
                DatabaseManager.Connection.Delete(playlist);
                Logger.Important($"Successfully deleted playlist: {playlist.Name} (#{playlist.Id})", LogType.Runtime);
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
            }

            Playlists.Remove(playlist);

            // Handle selection of the new playlist if deleted
            if (Selected.Value == playlist)
                Selected.Value = Playlists.Count != 0 ? Playlists.First() : null;

            PlaylistDeleted?.Invoke(typeof(PlaylistManager), new PlaylistDeletedEventArgs(playlist));
        }

        /// <summary>
        ///     Syncs playlist to an online map pool
        /// </summary>
        /// <param name="playlist"></param>
        public static void SyncPlaylistToMapPool(Playlist playlist)
        {
            if (playlist == null || !playlist.IsOnlineMapPool())
                return;

            var response = new APIRequestPlaylistMaps(playlist).ExecuteRequest();

            if (response == null)
            {
                NotificationManager.Show(NotificationLevel.Error, "Failed to sync playlist to map pool.");
                return;
            }

            // Create a dictionary for O(1) lookup of maps
            var allMaps = new Dictionary<int, Map>();
            foreach (var set in MapManager.Mapsets)
            {
                foreach (var map in set.Maps)
                {
                    if (!allMaps.ContainsKey(map.MapId))
                        allMaps[map.MapId] = map;
                }
            }

            var missingMapIds = new List<int>();

            foreach (var id in response.MapIds)
            {
                Map map = null;

                if (allMaps.TryGetValue(id, out var foundMap))
                    map = foundMap;

                // Map is already in playlist or doesn't exist
                if (map == null)
                {
                    missingMapIds.Add(id);
                    continue;
                }

                if (playlist.Maps.Any(x => x.MapId == id))
                    continue;

                AddMapToPlaylist(playlist, map);
            }

            if (missingMapIds.Count > 0)
            {
                Logger.Debug("Skipped following maps during playlist sync: " + String.Join(',', missingMapIds), LogType.Runtime);
                NotificationManager.Show(NotificationLevel.Info, $"Skipped {missingMapIds.Count} locally missing maps during playlist sync");
                HandleMissingMaps(playlist, missingMapIds);
            }

            Logger.Important($"Playlist {playlist.Name} (#{playlist.Id}) has been synced to map pool: {playlist.OnlineMapPoolId}", LogType.Runtime);
            PlaylistSynced?.Invoke(typeof(PlaylistManager), new PlaylistSyncedEventArgs(playlist));
        }

        /// <summary>
        ///     Handles missing maps by asking the user to download them
        /// </summary>
        /// <param name="missingMapIds"></param>
        private static void HandleMissingMaps(Playlist playlist, List<int> missingMapIds)
        {
            DialogManager.Show(new YesNoDialog("Missing Maps", "Do you want to download the missing maps?", () =>
            {
                NotificationManager.Show(NotificationLevel.Info, "Starting download of missing maps...");
                Task.Run(async () => await DownloadMissingMaps(playlist, missingMapIds));
            }));
        }

        /// <summary>
        ///     Downloads the missing maps
        /// </summary>
        /// <param name="missingMapIds"></param>
        private static async Task DownloadMissingMaps(Playlist playlist, List<int> missingMapIds)
        {
            var downloads = new List<MapsetDownload>();
            var processedMapsets = new HashSet<int>();

            foreach (var id in missingMapIds)
            {
                if (!OnlineManager.Connected)
                    return;

                try
                {
                    var response = new APIRequestMapInformation(id).ExecuteRequest();

                    if (response == null || response.Status != 200)
                        continue;

                    if (processedMapsets.Contains(response.Map.MapsetId))
                        continue;

                    processedMapsets.Add(response.Map.MapsetId);

                    if (MapsetDownloadManager.IsMapsetInQueue(response.Map.MapsetId))
                        continue;

                    MapsetDownload download = null;
                    var tcs = new TaskCompletionSource<bool>();

                    // Schedule the download on the main thread to prevent threading issues/deadlocks with the UI
                    ((WobbleGame)GameBase.Game).GlobalUserInterface.AddScheduledUpdate(() =>
                    {
                        try
                        {
                            download = MapsetDownloadManager.Download(response.Map.MapsetId, response.Map.Artist, response.Map.Title);
                        }
                        finally
                        {
                            tcs.SetResult(true);
                        }
                    });

                    // Wait for the main thread to process the download request
                    await tcs.Task;

                    if (download != null)
                        downloads.Add(download);

                    // Limit concurrent downloads to 7 to prevent connection issues
                    while (downloads.Count(x => !x.Status.Value.CancelledOrComplete) >= 7)
                        await Task.Delay(500);

                    // Delay to prevent spamming requests
                    await Task.Delay(100);
                }
                catch (Exception e)
                {
                    Logger.Error(e, LogType.Runtime);
                }
            }

            if (downloads.Count == 0)
                return;

            NotificationManager.Show(NotificationLevel.Success, ($"Started downloading {downloads.Count} missing mapsets!"));

            // Wait for all downloads to finish
            await Task.WhenAll(downloads.Select(WaitForDownload));

            // Done downloading. Trigger import.
            ThreadScheduler.Run(() =>
            {
                var game = (QuaverGame)GameBase.Game;

                // Only import if we're in a screen that allows it (Select, Menu, etc)
                // Using similar logic to QuaverIpcHandler
                if (game.CurrentScreen is SelectionScreen)
                    game.CurrentScreen.Exit(() => new ImportingScreen(null, true, false, null, () => SyncPlaylistToMapPool(playlist)));
                else if (game.CurrentScreen is MainMenuScreen)
                    game.CurrentScreen.Exit(() => new ImportingScreen(null, false, false, null, () => SyncPlaylistToMapPool(playlist)));
            });
        }

        /// <summary>
        ///     Waits for a download to complete or fail
        /// </summary>
        /// <param name="download"></param>
        /// <returns></returns>
        private static Task WaitForDownload(MapsetDownload download)
        {
            var tcs = new TaskCompletionSource<bool>();

            void Handler(object s, BindableValueChangedEventArgs<DownloadStatusChangedEventArgs> e)
            {
                if (e.Value.Status == FileDownloaderStatus.Complete || e.Value.CancelledOrComplete)
                {
                    download.Status.ValueChanged -= Handler;
                    tcs.TrySetResult(true);
                }
            }


            download.Status.ValueChanged += Handler;

            // Check if already done
            if (download.Status.Value != null && (download.Status.Value.Status == FileDownloaderStatus.Complete || download.Status.Value.CancelledOrComplete))
            {
                download.Status.ValueChanged -= Handler;
                tcs.TrySetResult(true);
            }

            return tcs.Task;
        }

        /// <summary>
        ///     Adds a map to a playlist
        /// </summary>
        /// <param name="playlist"></param>
        /// <param name="map"></param>
        public static void AddMapToPlaylist(Playlist playlist, Map map)
        {
            var playlistMap = new PlaylistMap()
            {
                PlaylistId = playlist.Id,
                Md5 = map.Md5Checksum
            };

            // Only add the map if it doesn't already exist
            if (!playlist.Maps.Contains(map) && playlist.Maps.All(x => x.Md5Checksum != map.Md5Checksum))
                playlist.Maps.Add(map);

            var check = DatabaseManager.Connection.Find<PlaylistMap>(y => y.PlaylistId == playlist.Id && y.Md5 == map.Md5Checksum);

            if (check == null)
                DatabaseManager.Connection.Insert(playlistMap);

            InvokePlaylistMapsManagedEvent(playlist);
        }

        /// <summary>
        ///     Removes an individual map from a playlist
        /// </summary>
        /// <param name="playlist"></param>
        /// <param name="map"></param>
        public static void RemoveMapFromPlaylist(Playlist playlist, Map map)
        {
            playlist.Maps.RemoveAll(x => x == map || x.Md5Checksum == map.Md5Checksum);

            var check = DatabaseManager.Connection.Find<PlaylistMap>(y => y.PlaylistId == playlist.Id && y.Md5 == map.Md5Checksum);

            if (check != null)
                DatabaseManager.Connection.Delete(check);

            InvokePlaylistMapsManagedEvent(playlist);
        }

        /// <summary>
        ///     Uploads the playlist to the server as a map pool
        /// </summary>
        /// <param name="playlist"></param>
        public static void UploadPlaylist(Playlist playlist)
        {
            Logger.Important($"Uploading playlist: {playlist.Name} (#{playlist.Id})", LogType.Runtime);

            var maps = playlist.Maps.FindAll(x => x.Game == MapGame.Quaver && x.MapId != -1);

            var ids = new List<int>();
            maps.ForEach(x => ids.Add(x.MapId));

            var response = OnlineManager.Client?.UploadPlaylist(playlist.Name, playlist.Description, ids);

            // Success
            if (response?.Status == 200)
            {
                playlist.OnlineMapPoolId = response.PlaylistId;
                playlist.OnlineMapPoolCreatorId = OnlineManager.Self.OnlineUser.Id;
                Logger.Important($"Successfully uploaded playlist: {playlist.Name} (#{playlist.Id}) online: {playlist.OnlineMapPoolId}", LogType.Runtime);

                var bannerPath = $"{ConfigManager.DataDirectory}/playlists/{playlist.Id}.jpg";

                if (!File.Exists(bannerPath))
                    bannerPath = $"{ConfigManager.DataDirectory}/playlists/{playlist.Id}.png";

                if (File.Exists(bannerPath))
                {
                    Logger.Important($"Uploading playlist background for {playlist.Name}...", LogType.Runtime);
                    var success = OnlineManager.Client.UploadPlaylistCover(playlist.OnlineMapPoolId, bannerPath);
                    if (success)
                        Logger.Important($"Successfully uploaded playlist background for {playlist.Name}!", LogType.Runtime);
                    else
                        Logger.Important($"Failed to upload playlist background for {playlist.Name}.", LogType.Runtime);
                }

                playlist.OpenUrl();
            }
            else
                Logger.Important($"Failed to upload playlist: {playlist.Name} (#{playlist.Id}) online!", LogType.Runtime);

            EditPlaylist(playlist, null);
        }

        /// <summary>
        ///     Updates the playlist online
        /// </summary>
        /// <param name="playlist"></param>
        /// <param name="removeMissingMaps"></param>
        public static void UpdatePlaylist(Playlist playlist, bool removeMissingMaps)
        {
            Logger.Important($"Updating playlist: {playlist.Name} (#{playlist.Id})", LogType.Runtime);

            var maps = playlist.Maps.FindAll(x => x.Game == MapGame.Quaver && x.MapId != -1);

            var ids = new List<int>();
            maps.ForEach(x => ids.Add(x.MapId));

            var response = OnlineManager.Client?.UpdatePlaylist(playlist.OnlineMapPoolId,
                playlist.Name, playlist.Description, ids, removeMissingMaps);

            // Success
            if (response == 200)
            {
                Logger.Important($"Successfully updated playlist: {playlist.Name} (#{playlist.Id}) online: {playlist.OnlineMapPoolId}", LogType.Runtime);

                var bannerPath = $"{ConfigManager.DataDirectory}/playlists/{playlist.Id}.jpg";

                if (!File.Exists(bannerPath))
                    bannerPath = $"{ConfigManager.DataDirectory}/playlists/{playlist.Id}.png";

                if (File.Exists(bannerPath))
                {
                    Logger.Important($"Uploading playlist background for {playlist.Name}...", LogType.Runtime);
                    var success = OnlineManager.Client.UploadPlaylistCover(playlist.OnlineMapPoolId, bannerPath);
                    if (success)
                        Logger.Important($"Successfully uploaded playlist background for {playlist.Name}!", LogType.Runtime);
                    else
                        Logger.Important($"Failed to upload playlist background for {playlist.Name}.", LogType.Runtime);
                }

                playlist.OpenUrl();
            }
            else
                Logger.Important($"Failed to upload playlist: {playlist.Name} (#{playlist.Id}) online! {playlist.OnlineMapPoolId}", LogType.Runtime);

            EditPlaylist(playlist, null);
        }

        /// <summary>
        ///     Removes a map from all playlists that its in
        /// </summary>
        /// <param name="map"></param>
        public static void RemoveMapFromAllPlaylists(Map map)
        {
            Playlists.ForEach(x =>
            {
                x.Maps.Remove(map);

                try
                {
                    var playlistMap = DatabaseManager.Connection.Find<PlaylistMap>(y => y.PlaylistId == x.Id && y.Md5 == map.Md5Checksum);

                    if (playlistMap != null)
                        DatabaseManager.Connection.Delete(playlistMap);
                }
                catch (Exception e)
                {
                    Logger.Error(e, LogType.Runtime);
                }
            });
        }

        /// <summary>
        ///     Updates a particular map in a playlist with
        /// </summary>
        /// <param name="outdated"></param>
        /// <param name="newMap"></param>
        public static void UpdateMapInPlaylists(Map outdated, Map newMap)
        {
            Playlists.ForEach(x =>
            {
                if (!x.Maps.Contains(outdated))
                    return;

                x.Maps.Remove(outdated);

                if (!x.Maps.Contains(newMap))
                    x.Maps.Add(newMap);

                try
                {
                    var playlistMap = DatabaseManager.Connection.Find<PlaylistMap>(y => y.PlaylistId == x.Id && y.Md5 == outdated.Md5Checksum);

                    if (playlistMap != null)
                        DatabaseManager.Connection.Delete(playlistMap);

                    AddMapToPlaylist(x, newMap);
                }
                catch (Exception e)
                {
                    Logger.Error(e, LogType.Runtime);
                }
            });
        }

        /// <summary>
        ///     Imports an online playlist
        /// </summary>
        /// <param name="onlineId"></param>
        public static void ImportPlaylist(int onlineId)
        {
            Logger.Important($"Importing playlist: ID {onlineId}", LogType.Runtime);
            PlaylistInformationResponse playlistResponse;

            try
            {
                playlistResponse = new APIRequestPlaylistInformation(onlineId).ExecuteRequest();
            }
            catch (Exception e)
            {
                Logger.Important($"Failed to retrieve playlist information: {e.StackTrace}", LogType.Network);
                NotificationManager.Show(NotificationLevel.Error, "Failed to retrieve playlist information");
                return;
            }

            if (playlistResponse == null || playlistResponse.Status != 200)
            {
                Logger.Important($"Failed to retrieve playlist information with error code: {playlistResponse?.Status ?? -1}", LogType.Network);
                NotificationManager.Show(NotificationLevel.Error, "Failed to retrieve playlist information");
                return;
            }

            var playlistAlreadyExistsLocally = false;

            var playlist = Playlists.Find(p => p.OnlineMapPoolId == onlineId);
            if (playlist == null)
            {
                playlist = new Playlist()
                {
                    Name = playlistResponse.PlaylistInformation.Name,
                    Description = playlistResponse.PlaylistInformation.Description,
                    Creator = playlistResponse.PlaylistInformation.OwnerUsername,
                    OnlineMapPoolId = playlistResponse.PlaylistInformation.Id,
                    OnlineMapPoolCreatorId = playlistResponse.PlaylistInformation.OwnerId
                };

                AddPlaylist(playlist);
            }
            else
            {
                Logger.Important($"Playlist with online ID #{playlist.OnlineMapPoolId} already exists locally", LogType.Runtime);

                playlist.Name = playlistResponse.PlaylistInformation.Name;
                playlist.Description = playlistResponse.PlaylistInformation.Description;
                playlist.Creator = playlistResponse.PlaylistInformation.OwnerUsername;

                EditPlaylist(playlist, null);

                playlistAlreadyExistsLocally = true;
            }

            SyncPlaylistToMapPool(playlist);

            var action = playlistAlreadyExistsLocally ? "updated" : "imported";
            NotificationManager.Show(NotificationLevel.Success, $"Successfully {action} playlist {playlist.Name}");

            ThreadScheduler.Run(() => DownloadPlaylistBackground(onlineId, playlist));
        }

        /// <summary>
        ///     Downloads the playlist background from the server
        /// </summary>
        /// <param name="id"></param>
        /// <param name="playlist"></param>
        private static void DownloadPlaylistBackground(int id, Playlist playlist)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
                var dl = new FileDownloader($"https://cdn.quavergame.com/playlists/{id}.jpg", tempPath);
                dl.Start().Wait();

                if (dl.Status == FileDownloaderStatus.Complete)
                {
                    CopyPlaylistBanner(playlist, tempPath);
                    File.Delete(tempPath);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to download playlist background for {id}: {e.Message}", LogType.Runtime);
            }
        }

        /// <summary>
        ///     Manually invokes an event that a playlist's maps have been managed
        /// </summary>
        /// <param name="playlist"></param>
        public static void InvokePlaylistMapsManagedEvent(Playlist playlist)
            => PlaylistMapsManaged?.Invoke(typeof(PlaylistManager), new PlaylistMapsManagedEventArgs(playlist));

        /// <summary>
        ///     Copies a banner path to the correct directory
        /// </summary>
        /// <param name="playlist"></param>
        /// <param name="bannerPath"></param>
        private static void CopyPlaylistBanner(Playlist playlist, string bannerPath)
        {
            if (bannerPath == null)
                return;

            // Dispose of old banner
            lock (BackgroundHelper.PlaylistBanners)
            {
                if (BackgroundHelper.PlaylistBanners.ContainsKey(playlist.Id.ToString()))
                {
                    var banner = BackgroundHelper.PlaylistBanners[playlist.Id.ToString()];

                    if (banner != UserInterface.DefaultBanner)
                        banner.Dispose();

                    BackgroundHelper.PlaylistBanners.Remove(playlist.Id.ToString());
                }
            }

            var dir = $"{ConfigManager.DataDirectory}/playlists";
            Directory.CreateDirectory(dir);
            File.Copy(bannerPath, $"{dir}/{playlist.Id}{Path.GetExtension(bannerPath)}", true);

            Logger.Important($"Copied over banner for playlist: {playlist.Id}", LogType.Runtime);
        }
    }
}