namespace Requestrr.WebApi.RequestrrBot.Books
{
    public class BookRequest
    {
        public int CategoryId { get; }
        public BookFormat Format { get; }
        public BookUserRequester User { get; }

        public BookRequest(BookUserRequester user, int categoryId, BookFormat format)
        {
            User = user;
            CategoryId = categoryId;
            Format = format;
        }
    }
}
