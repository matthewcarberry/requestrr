using Requestrr.WebApi.RequestrrBot.DownloadClients.Lidarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Ombi;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Overseerr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Radarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Readarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Sonarr;

namespace Requestrr.WebApi.config
{
    public class DownloadClientsSettings
    {
        public OmbiSettings Ombi { get; set; }
        public OverseerrSettings Overseerr { get; set; }
        public RadarrSettings Radarr { get; set; }
        public SonarrSettings Sonarr { get; set; }

        public LidarrSettings Lidarr { get; set; }

        public ReadarrSettings Bookshelf { get; set; }
        public ReadarrSettings Listenarr { get; set; }
    }
}