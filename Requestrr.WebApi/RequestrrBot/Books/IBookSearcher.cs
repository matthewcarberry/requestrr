using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Books
{
    public interface IBookSearcher
    {
        Task<IReadOnlyList<Book>> SearchBooksAsync(BookRequest request, string title);
        Task<Book> SearchBookAsync(BookRequest request, string bookId);
        Task<Dictionary<string, Book>> SearchAvailableBooksAsync(HashSet<string> bookIds, CancellationToken token);
    }
}
