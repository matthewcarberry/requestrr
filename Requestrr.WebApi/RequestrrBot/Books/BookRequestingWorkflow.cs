using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Books
{
    public class BookRequestingWorkflow
    {
        private readonly int _categoryId;
        private readonly BookFormat _format;
        private readonly BookUserRequester _user;
        private readonly IBookSearcher _bookSearcher;
        private readonly IBookRequester _requester;
        private readonly IBookUserInterface _userInterface;
        private readonly IBookNotificationWorkflow _notificationWorkflow;


        public BookRequestingWorkflow(
            BookUserRequester user,
            int categoryId,
            BookFormat format,
            IBookSearcher searcher,
            IBookRequester requester,
            IBookUserInterface userInterface,
            IBookNotificationWorkflow notificationWorkflow
        )
        {
            _categoryId = categoryId;
            _format = format;
            _user = user;
            _bookSearcher = searcher;
            _requester = requester;
            _userInterface = userInterface;
            _notificationWorkflow = notificationWorkflow;
        }


        private BookRequest CreateRequest()
        {
            return new BookRequest(_user, _categoryId, _format);
        }


        public async Task SearchBookAsync(string title)
        {
            IReadOnlyList<Book> books = await _bookSearcher.SearchBooksAsync(CreateRequest(), title.Trim());

            if (!books.Any())
            {
                await _userInterface.WarnNoBookFoundAsync(title);
            }
            else if (books.Count > 1)
            {
                await _userInterface.ShowBookSelection(CreateRequest(), books);
            }
            else
            {
                await HandleBookSelectionAsync(await _bookSearcher.SearchBookAsync(CreateRequest(), books.Single().BookId));
            }
        }


        public async Task HandleBookSelectionAsync(string bookId)
        {
            await HandleBookSelectionAsync(await _bookSearcher.SearchBookAsync(CreateRequest(), bookId));
        }


        private async Task HandleBookSelectionAsync(Book book)
        {
            if (CanBeRequested(book))
            {
                await _userInterface.DisplayBookDetailsAsync(CreateRequest(), book);
            }
            else if (book.Available)
            {
                await _userInterface.WarnBookAlreadyAvailableAsync(book);
            }
            else
            {
                await _notificationWorkflow.NotifyForExistingRequestAsync(_user.UserId, book);
            }
        }


        public async Task RequestBookAsync(string bookId)
        {
            Book book = await _bookSearcher.SearchBookAsync(CreateRequest(), bookId);
            BookRequestResult result = await _requester.RequestBookAsync(CreateRequest(), book);

            if (result.WasDenied)
            {
                await _userInterface.DisplayRequestDeniedAsync(book);
            }
            else
            {
                await _userInterface.DisplayRequestSuccessAsync(book);
                await _notificationWorkflow.NotifyForNewRequestAsync(_user.UserId, book);
            }
        }


        private static bool CanBeRequested(Book book)
        {
            return !book.Available && !book.Requested;
        }
    }
}
