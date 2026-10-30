using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.RequestrrBot.Books;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Readarr;
using System.Net.Http;

namespace Requestrr.WebApi.Controllers.DownloadClients.Readarr
{
    [ApiController]
    [Authorize]
    [Route("/api/audiobooks")]
    public class ListenarrClientController : ReadarrClientControllerBase
    {
        protected override BookFormat Format => BookFormat.Audiobook;

        public ListenarrClientController(IHttpClientFactory httpClientFactory, ILogger<ReadarrClient> logger, ReadarrSettingsProvider readarrSettingsProvider, BookSettingsProvider bookSettingsProvider)
            : base(httpClientFactory, logger, readarrSettingsProvider, bookSettingsProvider)
        {
        }
    }
}
