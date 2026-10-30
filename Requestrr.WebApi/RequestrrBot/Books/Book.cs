namespace Requestrr.WebApi.RequestrrBot.Books
{
    public class Book
    {
        public string DownloadClientId { get; set; }
        public string BookId { get; set; }
        public string Title { get; set; }
        public string AuthorName { get; set; }
        public string Overview { get; set; }
        public string ReleaseYear { get; set; }
        public BookFormat Format { get; set; }


        public bool Available { get; set; }
        public bool Monitored { get; set; }
        public bool Requested { get; set; }


        public string PosterPath { get; set; }
    }
}
