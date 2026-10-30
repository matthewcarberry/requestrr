using Requestrr.WebApi.Controllers.DownloadClients;
using Requestrr.WebApi.RequestrrBot.Books;
using Requestrr.WebApi.RequestrrBot.DownloadClients;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Readarr;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Movies;
using Requestrr.WebApi.RequestrrBot.Music;
using Requestrr.WebApi.RequestrrBot.TvShows;
using System.Collections.Generic;
using System.Linq;

namespace Requestrr.WebApi.Controllers.DownloadClients.Readarr
{
    /// <summary>
    /// Every category becomes a "/request [category]" command, so names must be unique across all request types
    /// </summary>
    public static class OtherCategories
    {
        /// <summary>
        /// Category names of the enabled Bookshelf/Listenarr client for a format
        /// </summary>
        public static IEnumerable<string> GetBookCategories(BookFormat format)
        {
            if (new BookSettingsProvider().Provide(format).Client != ReadarrSettingsProvider.ClientName(format))
                return Enumerable.Empty<string>();

            return new ReadarrSettingsProvider().Provide(format).Categories.Select(x => x.Name.ToLower());
        }


        /// <summary>
        /// Category names used by every other request type than the given book format
        /// </summary>
        public static IEnumerable<string> Get(BookFormat format)
        {
            List<string> categories = new List<string>();
            var downloadClientsSettings = new DownloadClientsSettingsProvider().Provide();

            switch (new MoviesSettingsProvider().Provide().Client)
            {
                case DownloadClient.Radarr:
                    categories.AddRange(downloadClientsSettings.Radarr.Categories.Select(x => x.Name.ToLower()));
                    break;
                case DownloadClient.Overseerr:
                    if (downloadClientsSettings.Overseerr.Movies.Categories.Any())
                        categories.AddRange(downloadClientsSettings.Overseerr.Movies.Categories.Select(x => x.Name.ToLower()));
                    else
                        categories.Add(Language.Current.DiscordCommandMovieRequestTitleName.ToLower());
                    break;
                case DownloadClient.Ombi:
                    categories.Add(Language.Current.DiscordCommandMovieRequestTitleName.ToLower());
                    break;
            }

            switch (new TvShowsSettingsProvider().Provide().Client)
            {
                case DownloadClient.Sonarr:
                    categories.AddRange(downloadClientsSettings.Sonarr.Categories.Select(x => x.Name.ToLower()));
                    break;
                case DownloadClient.Overseerr:
                    if (downloadClientsSettings.Overseerr.TvShows.Categories.Any())
                        categories.AddRange(downloadClientsSettings.Overseerr.TvShows.Categories.Select(x => x.Name.ToLower()));
                    else
                        categories.Add(Language.Current.DiscordCommandTvRequestTitleName.ToLower());
                    break;
                case DownloadClient.Ombi:
                    categories.Add(Language.Current.DiscordCommandTvRequestTitleName.ToLower());
                    break;
            }

            if (new MusicSettingsProvider().Provide().Client == DownloadClient.Lidarr)
                categories.AddRange(downloadClientsSettings.Lidarr.Categories.Select(x => x.Name.ToLower()));

            categories.AddRange(GetBookCategories(format == BookFormat.Ebook ? BookFormat.Audiobook : BookFormat.Ebook));

            return categories;
        }
    }
}
