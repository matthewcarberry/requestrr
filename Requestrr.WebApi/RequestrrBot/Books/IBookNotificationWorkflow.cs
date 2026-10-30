using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Books
{
    public interface IBookNotificationWorkflow
    {
        Task NotifyForNewRequestAsync(string userId, Book book);
        Task NotifyForExistingRequestAsync(string userId, Book book);
        Task AddNotificationAsync(string userId, string bookId);
    }
}
