using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Requestrr.WebApi.RequestrrBot.Books;
using Requestrr.WebApi.RequestrrBot.Notifications.TvShows;

namespace Requestrr.WebApi.RequestrrBot
{
    public static class NotificationsFile
    {
        private static object _lock = new object();

        public const string _notificationFile = "notifications.json";
        public static string FilePath { get => Path.Combine(SettingsFile.SettingsFolder, _notificationFile); }


        public static dynamic _cachedNotifications = null;

        private static bool _hasChanged = false;

        static NotificationsFile()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        lock (_lock)
                        {
                            if (_hasChanged)
                            {
                                File.WriteAllText(FilePath, JsonConvert.SerializeObject(_cachedNotifications));
                                _hasChanged = false;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            });
        }

        public static dynamic Read()
        {
            dynamic notifications = null;

            lock (_lock)
            {
                if (_cachedNotifications == null)
                {
                    _cachedNotifications = JObject.Parse(File.ReadAllText(FilePath));
                }

                notifications = _cachedNotifications;
            }

            return notifications;
        }

        public static void ClearAllTvShowNotifications()
        {
            lock (_lock)
            {
                _cachedNotifications.TvShows = JToken.FromObject(Array.Empty<int>());
                _hasChanged = true;
            }
        }

        public static void ClearAllMovieNotifications()
        {
            lock (_lock)
            {
                _cachedNotifications.Movies = JToken.FromObject(Array.Empty<int>());
                _hasChanged = true;
            }
        }


        /// <summary>
        /// Handles clearing all music notifications
        /// </summary>
        public static void ClearAllMusicNotifications()
        {
            lock (_lock)
            {
                _cachedNotifications.Music = JToken.FromObject(Array.Empty<int>());
                _hasChanged = true;
            }
        }


        public static void WriteMovies(Dictionary<string, int[]> moviesNotifications)
        {
            lock (_lock)
            {
                _cachedNotifications.Movies = JToken.FromObject(moviesNotifications.Select(x => new
                {
                    UserId = x.Key,
                    MovieIds = x.Value
                }).ToArray());

                _hasChanged = true;
            }
        }

        public static void WriteTvShows(Dictionary<string, TvShowNotification[]> tvShowsNotifications)
        {
            lock (_lock)
            {
                _cachedNotifications.TvShows = JToken.FromObject(tvShowsNotifications.Select(x => new
                {
                    UserId = x.Key,
                    Notifications = x.Value.Select(n =>
                        new
                        {
                            TvShowId = n.TvShowId,
                            SeasonNumber = n.SeasonNumber,
                            IsPermanent = n is FutureSeasonsNotification ? 1 : 0
                        }
                    ).ToArray()
                }).ToArray());
                _hasChanged = true;
            }
        }


        public static void WriteMusicArtist(Dictionary<string, string[]> musicNotifications)
        {
            lock (_lock)
            {
                _cachedNotifications.Music = JToken.FromObject(musicNotifications.Select(x => new
                {
                    UserId = x.Key,
                    MusicArtistId = x.Value
                }).ToArray());

                _hasChanged = true;
            }
        }


        /// <summary>
        /// Name of the notifications section for a book format
        /// </summary>
        private static string BookSectionName(BookFormat format)
        {
            return format == BookFormat.Audiobook ? "Audiobooks" : "Books";
        }


        /// <summary>
        /// Returns the book notifications for a format, files created before books were supported have no section yet
        /// </summary>
        public static JArray ReadBooks(BookFormat format)
        {
            dynamic notifications = Read();

            lock (_lock)
            {
                JObject root = (JObject)notifications;
                if (root[BookSectionName(format)] == null)
                    root[BookSectionName(format)] = new JArray();

                return (JArray)root[BookSectionName(format)];
            }
        }


        public static void ClearAllBookNotifications(BookFormat format)
        {
            lock (_lock)
            {
                ((JObject)Read())[BookSectionName(format)] = new JArray();
                _hasChanged = true;
            }
        }


        public static void WriteBooks(BookFormat format, Dictionary<string, string[]> bookNotifications)
        {
            lock (_lock)
            {
                ((JObject)_cachedNotifications)[BookSectionName(format)] = JToken.FromObject(bookNotifications.Select(x => new
                {
                    UserId = x.Key,
                    BookIds = x.Value
                }).ToArray());

                _hasChanged = true;
            }
        }
    }
}