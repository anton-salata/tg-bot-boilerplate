using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TgBotBoileplate.Service.Services
{
    public class BotService : BackgroundService
    {
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly ILogger<BotService> _logger;

        public BotService(ITelegramBotClient telegramBotClient, ILogger<BotService> logger)
        {
            _logger = logger;
            _telegramBotClient = telegramBotClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            ReceiverOptions receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _telegramBotClient.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, receiverOptions, stoppingToken);

            await SetBotCommands();

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public async Task SetBotCommands()
        {
            try
            {
                // Define custom commands
                var commands = new[]
                {
                    new BotCommand { Command = "start", Description = "Start using the bot" },
                    new BotCommand { Command = "legal", Description = "Get legal notices and terms of service" }
                };

                // Set custom commands for the bot
                await _telegramBotClient.SetMyCommandsAsync(commands);

                _logger.LogInformation("Custom commands set successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error setting custom commands: {ex.Message}");
            }
        }


        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Text)
                {
                    var message = update.Message;

                    // Check if the message contains a command
                    if (message.Entities != null && message.Entities.Any(e => e.Type == MessageEntityType.BotCommand))
                    {
                        // Extract the command text from the message
                        var commandEntity = message.Entities.First(e => e.Type == MessageEntityType.BotCommand);
                        var command = message.Text.Substring(commandEntity.Offset, commandEntity.Length);

                        // Handle the command
                        switch (command)
                        {
                            case "/start":
                                await HandleStartCommand(message);
                                break;
                            case "/legal":
                                await HandleLegalCommand(message);
                                break;
                            // Add other command handlers here as needed
                            default:
                                // Handle unknown command
                                await HandleUnknownCommand(message);
                                break;
                        }
                    }
                    else
                    {
                        // Handle non-command messages
                        await HandleTextMessage(message);
                    }
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    var callbackQuery = update.CallbackQuery;

                    // Handle inline keyboard button click
                    if (callbackQuery.Data.Equals("example-text"))
                    {
                        await HandleTextMessageExampleButtonClick(callbackQuery.Message);
                    }
                    else if (callbackQuery.Data.Equals("example-photo"))
                    {
                        await HandleSendPhotExample(callbackQuery.Message);
                    }
                    else if (callbackQuery.Data.StartsWith("example-file"))
                    {
                        await HandleExampleFileDownload(callbackQuery.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error handling update: {ex.Message}");
            }
        }

        private async Task HandleStartCommand(Message message)
        {
            try
            {
                var inlineKeyboardButtons = new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("Send Text Example", "example-text") },
                    new[] { InlineKeyboardButton.WithCallbackData("Send Photo Example", "example-photo") },
                    new[] { InlineKeyboardButton.WithCallbackData("Send File Example", "example-file") }
                };

                var inlineKeyboard = new InlineKeyboardMarkup(inlineKeyboardButtons);

                // Create inline keyboard markup with the generated buttons
                var inlineKeyboardMarkup = new InlineKeyboardMarkup(inlineKeyboardButtons);

                // Send the message with the list of countries and flag emojis
                await _telegramBotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Test Examples",
                    replyMarkup: inlineKeyboard
                );
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error handling /start command: {ex.Message}");
                // Inform the user about the error
                await _telegramBotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sorry, an error occurred while fetching test data. Please try again later."
                );
            }
        }


        private async Task HandleTextMessageExampleButtonClick(Message message)
        {
            try
            {
                // Send the text example
                await _telegramBotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Text example: You are the best!"
                );
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error handling Text Message Example button click: {ex.Message}");
                // Inform the user about the error
                await _telegramBotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sorry, an error occurred while sending test message. Please try again later."
                );
            }
        }

        private async Task HandleSendPhotExample(Message message)
        {
            try
            {
                var exampleImageBytes = await GetExampleImageBytes();

                // Send the image to the user
                using (MemoryStream stream = new MemoryStream(exampleImageBytes))
                {
                    var image = InputFile.FromStream(stream, "example.jpg");

                    await _telegramBotClient.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: image,
                        caption: "Example"
                    );
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error handling send phot example: {ex.Message}");
                // Inform the user about the error
                await _telegramBotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sorry, an error occurred while processing your request to show the example image. Please try again later."
                );
            }
        }

        private async Task HandleExampleFileDownload(Message message)
        {
            try
            {
                // Fetch configuration file based on the fileType parameter
                byte[] configFileBytes = await GetExampleFileBytes();

                // Send the configuration file to the user
                using (MemoryStream stream = new MemoryStream(configFileBytes))
                {
                    var inputFile = InputFile.FromStream(stream, $"ExampleFile.txt");

                    // Send the document to the user
                    await _telegramBotClient.SendDocumentAsync(
                        chatId: message.Chat.Id,
                        document: inputFile,
                        caption: $"Here is the ExampleFile.txt"
                    );
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error handling download example file: {ex.Message}");
                // Inform the user about the error
                await _telegramBotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Sorry, an error occurred while processing your request to download the sample file. Please try again later."
                );
            }
        }

        private async Task<byte[]> GetExampleImageBytes()
        {
            try
            {
                // Construct the path to the image
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string imagePath = Path.Combine(currentDirectory, "exampleimg.jpg");

                // Read the file as a byte array
                byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
                return imageBytes;
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error fetching example image bytes': {ex.Message}");
                return null;
            }
        }

        private async Task<byte[]> GetExampleFileBytes()
        {
            try
            {
                var exampleTextFileContent = "Believe you can and you're halfway there. - Theodore Roosevelt";

                // Convert the text to a byte array
                byte[] byteArray;
                using (var memoryStream = new MemoryStream())
                {
                    using (var writer = new StreamWriter(memoryStream, Encoding.UTF8))
                    {
                        await writer.WriteAsync(exampleTextFileContent);
                        await writer.FlushAsync();
                        memoryStream.Position = 0;
                        byteArray = memoryStream.ToArray();
                    }
                }

                return byteArray;
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Error example file generation: {ex.Message}");
                return null;
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };


            _logger.LogError(errorMessage);
            return Task.CompletedTask;
        }
        private async Task HandleLegalCommand(Message message)
        {
            // Implement logic to handle the /legal command
            // This command is used to provide legal notices and terms of service
        }

        private async Task HandleTextMessage(Message message)
        {
            // Implement logic to handle non-command text messages
            // This method is called when the message is not a command
        }

        private async Task HandleUnknownCommand(Message message)
        {
            // Implement logic to handle unknown commands
            // This method is called when the bot receives a command that it does not recognize
            // You can send a message to the user informing them that the command is not recognized
            // For example:
            // await _telegramBotClient.SendTextMessageAsync(message.Chat.Id, "Sorry, I don't understand that command.");
        }
    }
}