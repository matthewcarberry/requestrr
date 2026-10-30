using Microsoft.Extensions.Logging;
using Requestrr.WebApi.RequestrrBot.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Notifications.Books
{
    public class BookNotificationEngine
    {
        private readonly IBookSearcher _bookSearcher;
        private readonly IBookNotifier _notifier;
        private readonly ILogger _logger;
        private readonly BookNotificationsRepository _notificationsRepository;
        private Task _notificationTask = null;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public BookNotificationEngine(
            IBookSearcher bookSearcher,
            IBookNotifier notifier,
            ILogger logger,
            BookNotificationsRepository bookNotificationsRepository
        )
        {
            _bookSearcher = bookSearcher;
            _notifier = notifier;
            _logger = logger;
            _notificationsRepository = bookNotificationsRepository;
        }


        public void Start()
        {
            _notificationTask = Task.Run(async () =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        Dictionary<string, HashSet<string>> currentRequests = _notificationsRepository.GetAllBookNotifications();

                        if (currentRequests.Any())
                        {
                            Dictionary<string, Book> availableBooks = await _bookSearcher.SearchAvailableBooksAsync(new HashSet<string>(currentRequests.Keys), _tokenSource.Token);

                            foreach (KeyValuePair<string, HashSet<string>> request in currentRequests.Where(x => availableBooks.ContainsKey(x.Key)))
                            {
                                if (_tokenSource.IsCancellationRequested)
                                    return;

                                try
                                {
                                    HashSet<string> userNotified = await _notifier.NotifyBookAsync(request.Value.ToArray(), availableBooks[request.Key], _tokenSource.Token);

                                    foreach (string userId in userNotified)
                                    {
                                        _notificationsRepository.RemoveNotification(userId, request.Key);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "An error occurred while processing book notifications: " + ex.Message);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while retrieving all book notifications: " + ex.Message);
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), _tokenSource.Token);
                }
            }, _tokenSource.Token);
        }


        public async Task StopAsync()
        {
            try
            {
                _tokenSource.Cancel();
                await _notificationTask;
            }
            catch
            {
                _tokenSource.Dispose();
                _tokenSource = new CancellationTokenSource();
            }
        }
    }
}
