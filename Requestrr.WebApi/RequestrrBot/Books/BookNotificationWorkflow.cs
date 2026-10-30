using Requestrr.WebApi.RequestrrBot.Notifications.Books;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Books
{
    public class BookNotificationWorkflow : IBookNotificationWorkflow
    {
        private readonly BookNotificationsRepository _notificationsRepository;
        private readonly IBookUserInterface _userInterface;
        private readonly IBookSearcher _bookSearcher;
        private readonly BookFormat _format;
        private readonly bool _automaticNotificationForNewRequests;


        public BookNotificationWorkflow(
            BookNotificationsRepository bookNotificationsRepository,
            IBookUserInterface userInterface,
            IBookSearcher bookSearcher,
            BookFormat format,
            bool automaticNotificationForNewRequests
        )
        {
            _notificationsRepository = bookNotificationsRepository;
            _userInterface = userInterface;
            _bookSearcher = bookSearcher;
            _format = format;
            _automaticNotificationForNewRequests = automaticNotificationForNewRequests;
        }


        public Task NotifyForNewRequestAsync(string userId, Book book)
        {
            if (_automaticNotificationForNewRequests)
            {
                _notificationsRepository.AddNotification(userId, book.BookId);
            }

            return Task.CompletedTask;
        }


        public async Task NotifyForExistingRequestAsync(string userId, Book book)
        {
            if (_notificationsRepository.HasNotification(userId, book.BookId))
            {
                await _userInterface.WarnBookUnavailableAndAlreadyHasNotificationAsync(book);
            }
            else
            {
                await _userInterface.AskForNotificationRequestAsync(book);
            }
        }


        public async Task AddNotificationAsync(string userId, string bookId)
        {
            Book book = await _bookSearcher.SearchBookAsync(new BookRequest(null, int.MinValue, _format), bookId);
            _notificationsRepository.AddNotification(userId, book.BookId);
            await _userInterface.DisplayNotificationSuccessAsync(book);
        }
    }
}
