using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.RequestrrBot.Books;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Readarr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Requestrr.WebApi.Controllers.DownloadClients.Readarr
{
    /// <summary>
    /// Endpoints shared by the Bookshelf (/api/books) and Listenarr (/api/audiobooks) settings pages
    /// </summary>
    public abstract class ReadarrClientControllerBase : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReadarrClient> _logger;
        private readonly ReadarrSettingsProvider _readarrSettingsProvider;
        private readonly BookSettingsProvider _bookSettingsProvider;

        protected abstract BookFormat Format { get; }
        private string ClientName => ReadarrSettingsProvider.ClientName(Format);


        protected ReadarrClientControllerBase(
            IHttpClientFactory httpClientFactory,
            ILogger<ReadarrClient> logger,
            ReadarrSettingsProvider readarrSettingsProvider,
            BookSettingsProvider bookSettingsProvider)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _readarrSettingsProvider = readarrSettingsProvider;
            _bookSettingsProvider = bookSettingsProvider;
        }


        [HttpGet()]
        public IActionResult Get()
        {
            ReadarrSettings settings = _readarrSettingsProvider.Provide(Format);

            return Ok(new BookSettingsModel
            {
                Client = _bookSettingsProvider.Provide(Format).Client,
                Readarr = new ReadarrSettingsModel
                {
                    Hostname = settings.Hostname,
                    BaseUrl = settings.BaseUrl,
                    Port = settings.Port,
                    ApiKey = settings.ApiKey,
                    Categories = settings.Categories.Select(x => new ReadarrSettingsCategory
                    {
                        Id = x.Id,
                        Name = x.Name,
                        ProfileId = x.ProfileId,
                        MetadataProfileId = x.MetadataProfileId,
                        RootFolder = x.RootFolder,
                        Tags = x.Tags
                    }).ToArray(),
                    UseSSL = settings.UseSSL,
                    SearchNewRequests = settings.SearchNewRequests,
                    MonitorNewRequests = settings.MonitorNewRequests,
                    Version = settings.Version
                },
                OtherCategories = OtherCategories.Get(Format).ToArray()
            });
        }


        [HttpPost("disable")]
        public IActionResult Disable()
        {
            DownloadClientsSettingsRepository.SetDisabledClient(Format);
            return Ok(new { ok = true });
        }


        [HttpPost("client/test")]
        public async Task<IActionResult> TestSettings([FromBody] TestReadarrSettingsModel model)
        {
            try
            {
                await ReadarrClient.TestConnectionAsync(_httpClientFactory.CreateClient(), _logger, ConvertToReadarrSettings(model), ClientName);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("client/rootpath")]
        public async Task<IActionResult> GetRootPaths([FromBody] TestReadarrSettingsModel model)
        {
            try
            {
                IList<ReadarrClient.JSONRootPath> paths = await ReadarrClient.GetRootPaths(_httpClientFactory.CreateClient(), _logger, ConvertToReadarrSettings(model), ClientName);
                return Ok(paths.Select(x => new ReadarrPath { Id = x.id, Path = x.path }));
            }
            catch (Exception)
            {
                return BadRequest($"Could not load the paths from {ClientName}, check your settings.");
            }
        }


        [HttpPost("client/profile")]
        public async Task<IActionResult> GetProfiles([FromBody] TestReadarrSettingsModel model)
        {
            try
            {
                IList<ReadarrClient.JSONProfile> profiles = await ReadarrClient.GetProfiles(_httpClientFactory.CreateClient(), _logger, ConvertToReadarrSettings(model), ClientName);
                return Ok(profiles.Select(x => new ReadarrItem { Id = x.id, Name = x.name }));
            }
            catch (Exception)
            {
                return BadRequest($"Could not load profiles from {ClientName}, check your settings.");
            }
        }


        [HttpPost("client/metadataprofile")]
        public async Task<IActionResult> GetMetadataProfiles([FromBody] TestReadarrSettingsModel model)
        {
            try
            {
                IList<ReadarrClient.JSONProfile> profiles = await ReadarrClient.GetMetadataProfiles(_httpClientFactory.CreateClient(), _logger, ConvertToReadarrSettings(model), ClientName);
                return Ok(profiles.Select(x => new ReadarrItem { Id = x.id, Name = x.name }));
            }
            catch (Exception)
            {
                return BadRequest($"Could not load metadata profiles from {ClientName}, check your settings.");
            }
        }


        [HttpPost("client/tag")]
        public async Task<IActionResult> GetTags([FromBody] TestReadarrSettingsModel model)
        {
            try
            {
                IList<ReadarrClient.JSONTag> tags = await ReadarrClient.GetTags(_httpClientFactory.CreateClient(), _logger, ConvertToReadarrSettings(model), ClientName);
                return Ok(tags.Select(x => new ReadarrItem { Id = x.id, Name = x.label }));
            }
            catch (Exception)
            {
                return BadRequest($"Could not load tags from {ClientName}, check your settings.");
            }
        }


        [HttpPost("client")]
        public IActionResult Save([FromBody] ReadarrSettingsModel model)
        {
            if (!model.Categories.Any())
                return BadRequest("At least one category is required.");

            if (model.Categories.Any(x => string.IsNullOrWhiteSpace(x.Name)))
                return BadRequest("A category name is required");

            foreach (ReadarrSettingsCategory category in model.Categories)
            {
                category.Name = category.Name.Trim();
            }

            if (new HashSet<string>(model.Categories.Select(x => x.Name.ToLower())).Count != model.Categories.Length)
                return BadRequest("All categories must have different names.");

            if (new HashSet<int>(model.Categories.Select(x => x.Id)).Count != model.Categories.Length)
                return BadRequest("All categories must have different ids.");

            if (model.Categories.Any(x => !Regex.IsMatch(x.Name, @"^[\w-]{1,32}$")))
                return BadRequest("Invalid categorie names, make sure they only contain alphanumeric characters, dashes and underscores. (No spaces, etc)");

            HashSet<string> otherCategories = new HashSet<string>(OtherCategories.Get(Format));
            if (model.Categories.Any(x => otherCategories.Contains(x.Name.ToLower())))
                return BadRequest("A category name is already used by another request type, all categories must have different names.");

            DownloadClientsSettingsRepository.SetReadarr(Format, new ReadarrSettingsModel
            {
                Hostname = model.Hostname.Trim(),
                ApiKey = model.ApiKey.Trim(),
                BaseUrl = (model.BaseUrl ?? string.Empty).Trim(),
                Port = model.Port,
                Categories = model.Categories,
                SearchNewRequests = model.SearchNewRequests,
                MonitorNewRequests = model.MonitorNewRequests,
                UseSSL = model.UseSSL,
                Version = model.Version
            });

            return Ok(new { ok = true });
        }


        private static ReadarrSettings ConvertToReadarrSettings(TestReadarrSettingsModel model)
        {
            return new ReadarrSettings
            {
                ApiKey = model.ApiKey.Trim(),
                Hostname = model.Hostname.Trim(),
                BaseUrl = (model.BaseUrl ?? string.Empty).Trim(),
                Port = model.Port,
                UseSSL = model.UseSSL,
                Version = model.Version
            };
        }
    }
}
