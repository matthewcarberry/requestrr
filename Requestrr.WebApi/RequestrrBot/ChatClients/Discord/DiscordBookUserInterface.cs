using DSharpPlus;
using DSharpPlus.Entities;
using Requestrr.WebApi.RequestrrBot.Books;
using Requestrr.WebApi.RequestrrBot.Locale;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Requestrr.WebApi.RequestrrBot.ChatClients.Discord
{
    /// <summary>
    /// Discord messages for book and audiobook requests.
    /// Component ids are "BkRS" (selection), "BkRC" (confirm request) and "BkNR" (notify me),
    /// followed by the user id and the format code.
    /// </summary>
    public class DiscordBookUserInterface : IBookUserInterface
    {
        private readonly DiscordInteraction _interactionContext;
        private readonly BookFormat _format;

        public DiscordBookUserInterface(DiscordInteraction interactionContext, BookFormat format)
        {
            _interactionContext = interactionContext;
            _format = format;
        }


        public async Task ShowBookSelection(BookRequest request, IReadOnlyList<Book> books)
        {
            List<DiscordSelectComponentOption> options = books.Take(15).Select(x => new DiscordSelectComponentOption(GetFormattedBookTitle(x), $"{request.CategoryId}/{x.BookId}", string.IsNullOrWhiteSpace(x.AuthorName) ? null : LimitStringSize(x.AuthorName))).ToList();
            DiscordSelectComponent select = new DiscordSelectComponent($"BkRS/{_interactionContext.User.Id}/{_format.ToCode()}/{request.CategoryId}", LimitStringSize(Language.Current.DiscordCommandBookRequestHelpDropdown), options);

            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddComponents(select).WithContent(Language.Current.DiscordCommandBookRequestHelp));
        }


        public async Task WarnNoBookFoundAsync(string title)
        {
            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent(Language.Current.DiscordCommandBookNotFound.ReplaceTokens(LanguageTokens.BookTitle, title)));
        }


        public async Task DisplayBookDetailsAsync(BookRequest request, Book book)
        {
            DiscordButtonComponent requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"BkRC/{_interactionContext.User.Id}/{_format.ToCode()}/{request.CategoryId}/{book.BookId}", Language.Current.DiscordCommandRequestButton);
            await UpdateMessageAsync(book, requestButton, Language.Current.DiscordCommandBookRequestConfirm);
        }


        public async Task WarnBookAlreadyAvailableAsync(Book book)
        {
            DiscordButtonComponent availableButton = new DiscordButtonComponent(ButtonStyle.Primary, $"0/1/0", Language.Current.DiscordCommandAvailableButton, true);
            await UpdateMessageAsync(book, availableButton, Language.Current.DiscordCommandBookAlreadyAvailable);
        }


        public async Task DisplayRequestSuccessAsync(Book book)
        {
            DiscordButtonComponent successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandRequestButtonSuccess);
            await UpdateMessageAsync(book, successButton, Language.Current.DiscordCommandBookRequestSuccess.ReplaceTokens(book));
        }


        public async Task DisplayRequestDeniedAsync(Book book)
        {
            DiscordButtonComponent deniedButton = new DiscordButtonComponent(ButtonStyle.Danger, $"0/1/0", Language.Current.DiscordCommandRequestButtonDenied);
            await UpdateMessageAsync(book, deniedButton, Language.Current.DiscordCommandBookRequestDenied);
        }


        public async Task WarnBookUnavailableAndAlreadyHasNotificationAsync(Book book)
        {
            DiscordButtonComponent requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"0/1/0", Language.Current.DiscordCommandRequestButton, true);
            await UpdateMessageAsync(book, requestButton, Language.Current.DiscordCommandBookRequestAlreadyExistNotified);
        }


        public async Task AskForNotificationRequestAsync(Book book)
        {
            DiscordButtonComponent notificationButton = new DiscordButtonComponent(ButtonStyle.Primary, $"BkNR/{_interactionContext.User.Id}/{_format.ToCode()}/{book.BookId}", Language.Current.DiscordCommandNotifyMe, false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("🔔")));
            await UpdateMessageAsync(book, notificationButton, Language.Current.DiscordCommandBookNotificationRequest);
        }


        public async Task DisplayNotificationSuccessAsync(Book book)
        {
            DiscordButtonComponent successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandNotifyMeSuccess);
            await UpdateMessageAsync(book, successButton, Language.Current.DiscordCommandBookNotificationSuccess.ReplaceTokens(book));
        }



        public static DiscordEmbed GenerateBookDetails(Book book)
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle(string.IsNullOrWhiteSpace(book.ReleaseYear) ? book.Title : $"{book.Title} ({book.ReleaseYear})")
                .WithTimestamp(DateTime.Now)
                .WithFooter("Powered by Requestrr");

            if (int.TryParse(book.BookId, out _))
                embedBuilder.WithUrl($"https://www.goodreads.com/work/editions/{book.BookId}");

            if (!string.IsNullOrWhiteSpace(book.Overview))
                embedBuilder.WithDescription(book.Overview.Length > 255 ? book.Overview.Substring(0, 255) + "(...)" : book.Overview);

            if (!string.IsNullOrWhiteSpace(book.PosterPath) && book.PosterPath.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                embedBuilder.WithThumbnail(book.PosterPath);

            if (!string.IsNullOrWhiteSpace(book.AuthorName))
                embedBuilder.AddField($"__{Language.Current.DiscordEmbedBookAuthor}__", book.AuthorName, true);

            embedBuilder.AddField($"__{Language.Current.DiscordEmbedBookFormat}__", book.Format == BookFormat.Audiobook ? Language.Current.DiscordEmbedBookFormatAudiobook : Language.Current.DiscordEmbedBookFormatEbook, true);

            return embedBuilder.Build();
        }



        private async Task UpdateMessageAsync(Book book, DiscordButtonComponent button, string message)
        {
            DiscordWebhookBuilder builder = await AddPreviousDropdownsAsync(book, new DiscordWebhookBuilder().AddEmbed(GenerateBookDetails(book)));
            await _interactionContext.EditOriginalResponseAsync(builder.AddComponents(button).WithContent(message));
        }


        private string GetFormattedBookTitle(Book book)
        {
            string title = string.IsNullOrWhiteSpace(book.ReleaseYear) ? book.Title : $"{book.Title} ({book.ReleaseYear})";
            return LimitStringSize(title);
        }


        private static string LimitStringSize(string value, int limit = 100)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Length > limit ? value[..(limit - 3)] + "..." : value;
        }


        private async Task<DiscordWebhookBuilder> AddPreviousDropdownsAsync(Book book, DiscordWebhookBuilder builder)
        {
            DiscordSelectComponent previousBookSelector = (await _interactionContext.GetOriginalResponseAsync()).FilterComponents<DiscordSelectComponent>().FirstOrDefault();
            if (previousBookSelector != null)
            {
                DiscordSelectComponent bookSelector = new DiscordSelectComponent(previousBookSelector.CustomId, GetFormattedBookTitle(book), previousBookSelector.Options);
                builder.AddComponents(bookSelector);
            }

            return builder;
        }
    }
}
