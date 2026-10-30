using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.RequestrrBot.ChatClients.Discord;
using Requestrr.WebApi.RequestrrBot.DownloadClients;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Readarr;
using Requestrr.WebApi.RequestrrBot.Notifications;
using Requestrr.WebApi.RequestrrBot.Notifications.Books;
using System;
using System.Linq;

namespace Requestrr.WebApi.RequestrrBot.Books
{
    public class BookWorkflowFactory
    {
        private readonly DiscordSettingsProvider _settingsProvider;
        private readonly BookNotificationsRepository _bookNotificationsRepository;
        private readonly BookNotificationsRepository _audiobookNotificationsRepository;
        private readonly ReadarrClient _bookshelfClient;
        private readonly ReadarrClient _listenarrClient;


        public BookWorkflowFactory(
            DiscordSettingsProvider settingsProvider,
            BookNotificationsRepository bookNotificationsRepository,
            BookNotificationsRepository audiobookNotificationsRepository,
            ReadarrClient bookshelfClient,
            ReadarrClient listenarrClient
        )
        {
            _settingsProvider = settingsProvider;
            _bookNotificationsRepository = bookNotificationsRepository;
            _audiobookNotificationsRepository = audiobookNotificationsRepository;
            _bookshelfClient = bookshelfClient;
            _listenarrClient = listenarrClient;
        }


        public BookRequestingWorkflow CreateRequestingWorkflow(DiscordInteraction interaction, int categoryId, BookFormat format)
        {
            DiscordSettings settings = _settingsProvider.Provide();
            ReadarrClient client = GetClient(settings, format);

            return new BookRequestingWorkflow(
                new BookUserRequester(interaction.User.Id.ToString(), interaction.User.Username),
                categoryId,
                format,
                client,
                client,
                new DiscordBookUserInterface(interaction, format),
                CreateBookNotificationWorkflow(interaction, settings, format)
            );
        }


        public IBookNotificationWorkflow CreateNotificationWorkflow(DiscordInteraction interaction, BookFormat format)
        {
            return CreateBookNotificationWorkflow(interaction, _settingsProvider.Provide(), format);
        }


        public BookNotificationEngine CreateBookNotificationEngine(DiscordClient client, ILogger logger, BookFormat format)
        {
            DiscordSettings settings = _settingsProvider.Provide();
            IBookNotifier bookNotifier = null;

            if (settings.NotificationMode == NotificationMode.PrivateMessage)
                bookNotifier = new PrivateMessageBookNotifier(client, logger);
            else if (settings.NotificationMode == NotificationMode.Channels)
                bookNotifier = new ChannelBookNotifier(client, settings.NotificationChannels.Select(x => ulong.Parse(x)).ToArray(), logger);
            else
                throw new Exception($"Could not create book notifier of type \"{settings.NotificationMode}\"");

            return new BookNotificationEngine(GetClient(settings, format), bookNotifier, logger, GetRepository(format));
        }


        private IBookNotificationWorkflow CreateBookNotificationWorkflow(DiscordInteraction interaction, DiscordSettings settings, BookFormat format)
        {
            DiscordBookUserInterface userInterface = new DiscordBookUserInterface(interaction, format);

            if (settings.NotificationMode == NotificationMode.Disabled)
                return new DisabledBookNotificationWorkflow(userInterface);

            return new BookNotificationWorkflow(GetRepository(format), userInterface, GetClient(settings, format), format, settings.AutomaticallyNotifyRequesters);
        }


        private BookNotificationsRepository GetRepository(BookFormat format)
        {
            return format == BookFormat.Audiobook ? _audiobookNotificationsRepository : _bookNotificationsRepository;
        }


        private ReadarrClient GetClient(DiscordSettings settings, BookFormat format)
        {
            if (format == BookFormat.Ebook && settings.BookDownloadClient == DownloadClient.Bookshelf)
                return _bookshelfClient;

            if (format == BookFormat.Audiobook && settings.AudiobookDownloadClient == DownloadClient.Listenarr)
                return _listenarrClient;

            throw new Exception($"Invalid configured {format} download client");
        }
    }
}
