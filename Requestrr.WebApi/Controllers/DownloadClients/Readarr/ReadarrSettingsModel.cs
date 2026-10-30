using System;
using System.ComponentModel.DataAnnotations;

namespace Requestrr.WebApi.Controllers.DownloadClients.Readarr
{
    public class TestReadarrSettingsModel
    {
        [Required]
        public string Hostname { get; set; }
        [Required]
        public int Port { get; set; }
        [Required]
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        [Required]
        public bool UseSSL { get; set; }
        [Required]
        public string Version { get; set; }
    }


    public class ReadarrSettingsModel : TestReadarrSettingsModel
    {
        [Required]
        public ReadarrSettingsCategory[] Categories { get; set; } = Array.Empty<ReadarrSettingsCategory>();

        public bool SearchNewRequests { get; set; }
        public bool MonitorNewRequests { get; set; }
    }


    public class ReadarrSettingsCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProfileId { get; set; }
        public int MetadataProfileId { get; set; }
        public string RootFolder { get; set; }
        public int[] Tags { get; set; } = Array.Empty<int>();
    }


    public class ReadarrItem
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
    }


    public class ReadarrPath
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Path { get; set; }
    }


    public class BookSettingsModel
    {
        [Required]
        public string Client { get; set; }

        [Required]
        public ReadarrSettingsModel Readarr { get; set; }

        [Required]
        public string[] OtherCategories { get; set; }
    }
}
