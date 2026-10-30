using Requestrr.WebApi.RequestrrBot.Books;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Requestrr.WebApi.RequestrrBot.Notifications.Books
{
    /// <summary>
    /// Pending "notify me" requests, kept separately for each book format
    /// </summary>
    public class BookNotificationsRepository
    {
        private class Notification
        {
            public string UserId { get; }
            public string BookId { get; }
            public Notification(string userId, string bookId)
            {
                UserId = userId;
                BookId = bookId;
            }

            public override bool Equals(object obj)
            {
                return obj is Notification notification &&
                    UserId == notification.UserId &&
                    BookId == notification.BookId;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(UserId, BookId);
            }
        }


        private readonly BookFormat _format;
        private HashSet<Notification> _notifications = new HashSet<Notification>();
        private object _lock = new object();

        public BookNotificationsRepository(BookFormat format)
        {
            _format = format;

            foreach (dynamic notification in NotificationsFile.ReadBooks(format))
            {
                foreach (var bookId in notification.BookIds)
                {
                    _notifications.Add(new Notification(notification.UserId.ToString(), (string)bookId));
                }
            }
        }



        public void AddNotification(string userId, string bookId)
        {
            lock (_lock)
            {
                if (_notifications.Add(new Notification(userId, bookId)))
                {
                    Save();
                }
            }
        }



        public void RemoveNotification(string userId, string bookId)
        {
            lock (_lock)
            {
                if (_notifications.Remove(new Notification(userId, bookId)))
                {
                    Save();
                }
            }
        }


        public Dictionary<string, HashSet<string>> GetAllBookNotifications()
        {
            lock (_lock)
            {
                return _notifications
                    .GroupBy(x => x.BookId)
                    .ToDictionary(x => x.Key, x => new HashSet<string>(x.Select(y => y.UserId)));
            }
        }


        public bool HasNotification(string userId, string bookId)
        {
            lock (_lock)
            {
                return _notifications.Contains(new Notification(userId, bookId));
            }
        }


        private void Save()
        {
            NotificationsFile.WriteBooks(_format, _notifications.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.Select(y => y.BookId).ToArray()));
        }
    }
}
