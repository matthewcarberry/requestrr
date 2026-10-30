using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Requestrr.WebApi.Extensions;
using Requestrr.WebApi.RequestrrBot.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.DownloadClients.Readarr
{
    /// <summary>
    /// Client for the Readarr v1 API, which Bookshelf and its Listenarr fork both expose.
    /// One instance is created per format, Bookshelf for ebooks and Listenarr for audiobooks.
    /// </summary>
    public class ReadarrClient : IBookSearcher, IBookRequester
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReadarrClient> _logger;
        private readonly ReadarrSettingsProvider _settingsProvider;
        private readonly BookFormat _format;

        private ReadarrSettings _settings => _settingsProvider.Provide(_format);
        private string BaseURL => GetBaseURL(_settings);
        private string ClientName => ReadarrSettingsProvider.ClientName(_format);


        public ReadarrClient(IHttpClientFactory httpClientFactory, ILogger<ReadarrClient> logger, ReadarrSettingsProvider settingsProvider, BookFormat format)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _settingsProvider = settingsProvider;
            _format = format;
        }



        /// <summary>
        /// Used to test if the Bookshelf/Listenarr service can be found
        /// </summary>
        public static async Task TestConnectionAsync(HttpClient httpClient, ILogger<ReadarrClient> logger, ReadarrSettings settings, string clientName)
        {
            if (!string.IsNullOrWhiteSpace(settings.BaseUrl) && !settings.BaseUrl.StartsWith("/"))
            {
                throw new Exception("Invalid base URL, must start with /");
            }

            try
            {
                HttpResponseMessage response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/config/host");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new Exception("Invalid api key");
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new Exception("Incorrect api version");
                }

                try
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JObject.Parse(responseString);

                    if (!jsonResponse.urlBase.ToString().Equals(settings.BaseUrl ?? string.Empty, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new Exception();
                    }
                }
                catch
                {
                    throw new Exception($"Base url does not match what is set in {clientName}");
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, $"Error while testing {clientName} connection: " + ex.Message);
                throw new Exception("Invalid host and/or port");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error while testing {clientName} connection: " + ex.Message);

                if (ex.GetType() == typeof(Exception))
                {
                    throw;
                }

                throw new Exception("Invalid host and/or port");
            }
        }


        public static Task<IList<JSONRootPath>> GetRootPaths(HttpClient httpClient, ILogger<ReadarrClient> logger, ReadarrSettings settings, string clientName)
        {
            return GetListAsync<JSONRootPath>(httpClient, logger, settings, "rootfolder", $"{clientName} root paths");
        }


        public static Task<IList<JSONProfile>> GetProfiles(HttpClient httpClient, ILogger<ReadarrClient> logger, ReadarrSettings settings, string clientName)
        {
            return GetListAsync<JSONProfile>(httpClient, logger, settings, "qualityprofile", $"{clientName} profiles");
        }


        public static Task<IList<JSONProfile>> GetMetadataProfiles(HttpClient httpClient, ILogger<ReadarrClient> logger, ReadarrSettings settings, string clientName)
        {
            return GetListAsync<JSONProfile>(httpClient, logger, settings, "metadataprofile", $"{clientName} metadata profiles");
        }


        public static Task<IList<JSONTag>> GetTags(HttpClient httpClient, ILogger<ReadarrClient> logger, ReadarrSettings settings, string clientName)
        {
            return GetListAsync<JSONTag>(httpClient, logger, settings, "tag", $"{clientName} tags");
        }


        private static async Task<IList<T>> GetListAsync<T>(HttpClient httpClient, ILogger<ReadarrClient> logger, ReadarrSettings settings, string resource, string description)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/{resource}");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IList<T>>(jsonResponse);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"An error while getting {description}: " + ex.Message);
            }

            throw new Exception($"An error occurred while getting {description}");
        }



        public async Task<IReadOnlyList<Book>> SearchBooksAsync(BookRequest request, string title)
        {
            try
            {
                List<JSONBook> books = await LookupBooksAsync(title.Trim());
                return books
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ForeignBookId))
                    .GroupBy(x => x.ForeignBookId)
                    .Select(x => ConvertToBook(x.First()))
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while searching for books with {ClientName}: " + ex.Message);
            }

            throw new Exception($"An error occurred while searching for books with {ClientName}");
        }



        public async Task<Book> SearchBookAsync(BookRequest request, string bookId)
        {
            try
            {
                JSONBook book = await FindExistingBookAsync(bookId);

                if (book == null)
                {
                    List<JSONBook> books = await LookupBooksAsync(GetLookupTerm(bookId));
                    book = books.FirstOrDefault(x => x.ForeignBookId == bookId) ?? books.First();
                }

                return ConvertToBook(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while searching for book by Id \"{bookId}\" with {ClientName}: {ex.Message}");
            }

            throw new Exception($"An error occurred while searching for book by Id with {ClientName}");
        }



        public async Task<Dictionary<string, Book>> SearchAvailableBooksAsync(HashSet<string> bookIds, CancellationToken token)
        {
            try
            {
                List<Book> books = new List<Book>();

                foreach (string bookId in bookIds)
                {
                    if (token.IsCancellationRequested)
                        break;

                    JSONBook existingBook = await FindExistingBookAsync(bookId);
                    if (existingBook != null)
                        books.Add(ConvertToBook(existingBook));
                }

                return books.Where(x => x.Available).ToDictionary(x => x.BookId, x => x);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while searching available books with {ClientName}: " + ex.Message);
            }

            throw new Exception($"An error occurred while searching available books with {ClientName}");
        }



        public async Task<BookRequestResult> RequestBookAsync(BookRequest request, Book book)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(book.DownloadClientId))
                    await CreateBookAsync(request, book);
                else
                    await UpdateExistingBookAsync(request, book);

                return new BookRequestResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error while requesting book \"{book.Title}\" from {ClientName}: " + ex.Message);
            }

            throw new Exception($"An error occurred while requesting a book from {ClientName}");
        }



        private ReadarrCategory GetCategory(BookRequest request, Book book)
        {
            ReadarrCategory category = _settings.Categories.SingleOrDefault(x => x.Id == request.CategoryId);

            if (category == null)
            {
                _logger.LogError($"An error occurred while requesting book \"{book.Title}\" from {ClientName}, could not find category with id {request.CategoryId}");
                throw new Exception($"An error occurred while requesting book \"{book.Title}\" from {ClientName}, could not find category with id {request.CategoryId}");
            }

            return category;
        }



        /// <summary>
        /// Adds the book (and its author when needed) the same way the Bookshelf UI does:
        /// the search result is posted back with the category's profiles and root folder.
        /// This uses /search rather than /book/lookup because only /search includes the editions and author that adding a book needs.
        /// </summary>
        private async Task CreateBookAsync(BookRequest request, Book book)
        {
            ReadarrCategory category = GetCategory(request, book);
            ReadarrSettings settings = _settings;

            HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/search?term={Uri.EscapeDataString(GetLookupTerm(book.BookId))}");
            await response.ThrowIfNotSuccessfulAsync($"{ClientName}BookSearch failed", x => x.message);

            List<JObject> searchResults = JArray.Parse(await response.Content.ReadAsStringAsync())
                .Select(x => x["book"] as JObject)
                .Where(x => x != null)
                .ToList();
            JObject newBook = searchResults.FirstOrDefault(x => x.Value<string>("foreignBookId") == book.BookId) ?? searchResults.First();

            JObject author = (JObject)newBook["author"];
            if (author != null && (author.Value<int?>("id") ?? 0) == 0)
            {
                author["qualityProfileId"] = category.ProfileId;
                author["metadataProfileId"] = category.MetadataProfileId;
                author["rootFolderPath"] = category.RootFolder;
                author["tags"] = JToken.FromObject(category.Tags);
                author["monitored"] = true;
                author["monitorNewItems"] = "none";
                author["addOptions"] = JToken.FromObject(new
                {
                    booksToMonitor = new[] { book.BookId },
                    searchForMissingBooks = false
                });
            }

            newBook["monitored"] = settings.MonitorNewRequests;
            newBook["addOptions"] = JToken.FromObject(new
            {
                searchForNewBook = settings.SearchNewRequests
            });

            response = await HttpPostAsync($"{BaseURL}/book", newBook.ToString(Formatting.None));
            await response.ThrowIfNotSuccessfulAsync($"{ClientName}BookCreation failed", x => x.message);
        }



        private async Task UpdateExistingBookAsync(BookRequest request, Book book)
        {
            int bookId = int.Parse(book.DownloadClientId);
            ReadarrSettings settings = _settings;

            HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/book/{bookId}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    await CreateBookAsync(request, book);
                    return;
                }

                await response.ThrowIfNotSuccessfulAsync($"{ClientName}GetBook failed", x => x.message);
            }

            response = await HttpPutAsync($"{BaseURL}/book/monitor", JsonConvert.SerializeObject(new
            {
                bookIds = new[] { bookId },
                monitored = true
            }));
            await response.ThrowIfNotSuccessfulAsync($"{ClientName}MonitorBook failed", x => x.message);

            if (settings.SearchNewRequests)
            {
                response = await HttpPostAsync($"{BaseURL}/command", JsonConvert.SerializeObject(new
                {
                    name = "BookSearch",
                    bookIds = new[] { bookId }
                }));

                await response.ThrowIfNotSuccessfulAsync($"{ClientName}BookSearchCommand failed", x => x.message);
            }
        }



        private async Task<List<JSONBook>> LookupBooksAsync(string term)
        {
            HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/book/lookup?term={Uri.EscapeDataString(term)}");
            await response.ThrowIfNotSuccessfulAsync($"{ClientName}BookLookup failed", x => x.message);

            return JsonConvert.DeserializeObject<List<JSONBook>>(await response.Content.ReadAsStringAsync()) ?? new List<JSONBook>();
        }



        /// <summary>
        /// Books in the library can be found by their title slug, which Bookshelf sets to the foreign book id
        /// </summary>
        private async Task<JSONBook> FindExistingBookAsync(string bookId)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/book?titleSlug={Uri.EscapeDataString(bookId)}");
                await response.ThrowIfNotSuccessfulAsync("Could not search book by Id", x => x.message);

                List<JSONBook> books = JsonConvert.DeserializeObject<List<JSONBook>>(await response.Content.ReadAsStringAsync());
                return books?.FirstOrDefault(x => x.ForeignBookId == bookId) ?? books?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred finding existing book by Id \"{bookId}\" with {ClientName}: {ex.Message}");
            }

            return null;
        }


        /// <summary>
        /// Numeric ids are work ids, which the lookup endpoint resolves with the "work:" prefix
        /// </summary>
        private static string GetLookupTerm(string bookId)
        {
            return int.TryParse(bookId, out _) ? $"work:{bookId}" : bookId;
        }



        private Task<HttpResponseMessage> HttpGetAsync(string url)
        {
            return HttpGetAsync(_httpClientFactory.CreateClient(), _settings, url);
        }


        private static async Task<HttpResponseMessage> HttpGetAsync(HttpClient client, ReadarrSettings settings, string url)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("X-Api-Key", settings.ApiKey);

            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
            {
                return await client.SendAsync(request, cts.Token);
            }
        }


        private Task<HttpResponseMessage> HttpPostAsync(string url, string content)
        {
            return HttpSendAsync(HttpMethod.Post, url, content);
        }


        private Task<HttpResponseMessage> HttpPutAsync(string url, string content)
        {
            return HttpSendAsync(HttpMethod.Put, url, content);
        }


        private async Task<HttpResponseMessage> HttpSendAsync(HttpMethod method, string url, string content)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("X-Api-Key", _settings.ApiKey);

            return await _httpClientFactory.CreateClient().SendAsync(request);
        }


        private static string GetBaseURL(ReadarrSettings settings)
        {
            string protocol = settings.UseSSL ? "https" : "http";

            return $"{protocol}://{settings.Hostname}:{settings.Port}{settings.BaseUrl}/api/v{settings.Version}";
        }



        private Book ConvertToBook(JSONBook jsonBook)
        {
            string downloadClientId = (jsonBook.Id ?? 0) > 0 ? jsonBook.Id.ToString() : string.Empty;

            return new Book
            {
                DownloadClientId = downloadClientId,
                BookId = jsonBook.ForeignBookId,
                Title = jsonBook.Title,
                AuthorName = jsonBook.Author?.AuthorName ?? string.Empty,
                Overview = jsonBook.Overview,
                ReleaseYear = jsonBook.ReleaseDate?.Year.ToString() ?? string.Empty,
                Format = _format,

                Available = (jsonBook.Statistics?.BookFileCount ?? 0) > 0,
                Monitored = jsonBook.Monitored,
                Requested = !string.IsNullOrWhiteSpace(downloadClientId) && jsonBook.Monitored,

                PosterPath = GetCoverUrl(jsonBook)
            };
        }


        private static string GetCoverUrl(JSONBook jsonBook)
        {
            if (!string.IsNullOrWhiteSpace(jsonBook.RemoteCover))
                return jsonBook.RemoteCover;

            JSONImage cover = jsonBook.Images?.FirstOrDefault(x => x.CoverType != null && x.CoverType.Equals("cover", StringComparison.InvariantCultureIgnoreCase));
            if (cover == null)
                return string.Empty;

            return !string.IsNullOrWhiteSpace(cover.RemoteUrl) ? cover.RemoteUrl : cover.Url;
        }



        public class JSONRootPath
        {
            public string path { get; set; }
            public int id { get; set; }
        }

        public class JSONProfile
        {
            public string name { get; set; }
            public int id { get; set; }
        }

        public class JSONTag
        {
            public string label { get; set; }
            public int id { get; set; }
        }


        private class JSONImage
        {
            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("coverType")]
            public string CoverType { get; set; }

            [JsonProperty("remoteUrl")]
            public string RemoteUrl { get; set; }
        }

        private class JSONStatistics
        {
            [JsonProperty("bookFileCount")]
            public int BookFileCount { get; set; }

            [JsonProperty("sizeOnDisk")]
            public long SizeOnDisk { get; set; }
        }

        private class JSONAuthor
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("authorName")]
            public string AuthorName { get; set; }

            [JsonProperty("foreignAuthorId")]
            public string ForeignAuthorId { get; set; }
        }

        private class JSONBook
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("overview")]
            public string Overview { get; set; }

            [JsonProperty("foreignBookId")]
            public string ForeignBookId { get; set; }

            [JsonProperty("monitored")]
            public bool Monitored { get; set; }

            [JsonProperty("releaseDate")]
            public DateTime? ReleaseDate { get; set; }

            [JsonProperty("author")]
            public JSONAuthor Author { get; set; }

            [JsonProperty("images")]
            public List<JSONImage> Images { get; set; }

            [JsonProperty("remoteCover")]
            public string RemoteCover { get; set; }

            [JsonProperty("statistics")]
            public JSONStatistics Statistics { get; set; }
        }
    }
}
