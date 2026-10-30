namespace Requestrr.WebApi.RequestrrBot.Books
{
    public class BookSettingsProvider
    {
        /// <summary>
        /// Name of the top level settings section holding the client for a format
        /// </summary>
        public static string SectionName(BookFormat format)
        {
            return format == BookFormat.Audiobook ? "Audiobooks" : "Books";
        }

        public BookSettings Provide(BookFormat format)
        {
            dynamic settings = SettingsFile.Read();

            return new BookSettings
            {
                Client = settings[SectionName(format)].Client
            };
        }
    }
}
