namespace Requestrr.WebApi.RequestrrBot.Books
{
    /// <summary>
    /// Ebooks are requested through Bookshelf, audiobooks through Listenarr
    /// </summary>
    public enum BookFormat
    {
        Ebook,
        Audiobook
    }

    public static class BookFormatExtensions
    {
        /// <summary>
        /// Short code used inside Discord component ids
        /// </summary>
        public static string ToCode(this BookFormat format)
        {
            return format == BookFormat.Audiobook ? "a" : "e";
        }

        public static BookFormat FromCode(string code)
        {
            return code == "a" ? BookFormat.Audiobook : BookFormat.Ebook;
        }
    }
}
