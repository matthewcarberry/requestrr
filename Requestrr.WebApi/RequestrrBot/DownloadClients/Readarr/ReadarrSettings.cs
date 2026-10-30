using System;

namespace Requestrr.WebApi.RequestrrBot.DownloadClients.Readarr
{
    /// <summary>
    /// Connection settings for a Readarr API compatible client (Bookshelf or Listenarr)
    /// </summary>
    public class ReadarrSettings
    {
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public ReadarrCategory[] Categories { get; set; } = Array.Empty<ReadarrCategory>();
        public bool SearchNewRequests { get; set; }
        public bool MonitorNewRequests { get; set; }
        public bool UseSSL { get; set; }
        public string Version { get; set; }
    }

    public class ReadarrCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProfileId { get; set; }
        public int MetadataProfileId { get; set; }
        public string RootFolder { get; set; }
        public int[] Tags { get; set; } = Array.Empty<int>();
    }
}
