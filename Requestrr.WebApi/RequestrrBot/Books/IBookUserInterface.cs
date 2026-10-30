using System.Collections.Generic;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Books
{
    public interface IBookUserInterface
    {
        Task ShowBookSelection(BookRequest request, IReadOnlyList<Book> books);
        Task WarnNoBookFoundAsync(string title);

        Task DisplayBookDetailsAsync(BookRequest request, Book book);
        Task DisplayRequestDeniedAsync(Book book);
        Task DisplayRequestSuccessAsync(Book book);

        Task WarnBookAlreadyAvailableAsync(Book book);

        Task WarnBookUnavailableAndAlreadyHasNotificationAsync(Book book);
        Task AskForNotificationRequestAsync(Book book);
        Task DisplayNotificationSuccessAsync(Book book);
    }
}
