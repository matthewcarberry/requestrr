using Requestrr.WebApi.RequestrrBot.Books;

namespace Requestrr.WebApi.RequestrrBot.DownloadClients.Readarr
{
    public class ReadarrSettingsProvider
    {
        /// <summary>
        /// Name of the DownloadClients settings section for a format
        /// </summary>
        public static string ClientName(BookFormat format)
        {
            return format == BookFormat.Audiobook ? DownloadClient.Listenarr : DownloadClient.Bookshelf;
        }

        public ReadarrSettings Provide(BookFormat format)
        {
            dynamic settings = SettingsFile.Read();
            dynamic clientSettings = settings.DownloadClients[ClientName(format)];

            return new ReadarrSettings
            {
                Hostname = clientSettings.Hostname,
                BaseUrl = clientSettings.BaseUrl,
                Port = (int)clientSettings.Port,
                ApiKey = clientSettings.ApiKey,
                Categories = clientSettings.Categories.ToObject<ReadarrCategory[]>(),
                SearchNewRequests = clientSettings.SearchNewRequests,
                MonitorNewRequests = clientSettings.MonitorNewRequests,
                UseSSL = (bool)clientSettings.UseSSL,
                Version = clientSettings.Version
            };
        }
    }
}
