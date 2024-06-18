using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Telegram.Bot;
using TgBotBoileplate.Service.Common;
using TgBotBoileplate.Service.Models;
using TgBotBoileplate.Service.Logging;
using TgBotBoileplate.DAL.Repositories;
using TgBotBoileplate.Service.Services;
using TgBotBoileplate.DAL.Models;

namespace TgBotBoileplate.Service
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureLogging(x =>
            {
                x.ClearProviders();
                x.AddConsole();
            })
             .ConfigureAppConfiguration((hostingContext, config) =>
             {
                 // Add additional configuration sources here
                 config.SetBasePath(Directory.GetCurrentDirectory())
                       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                       .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true)
                       .AddEnvironmentVariables();
             })
            .UseSystemd()
            //.UseWindowsService()
            .ConfigureServices((hostContext, services) =>
            {

                // Configure AppSettings
                var configuration = hostContext.Configuration;
                services.Configure<AppSettings>(configuration);

                services.AddSingleton(provider =>
                {
                    // Retrieve the configured MongoDB connection string 
                    var connectionString = configuration[Constants.MONGO_CONNECTION_STRING_SETTING_NAME];

                    // Parse the database name from the connection string
                    var mongoUrl = new MongoUrl(connectionString);
                    var databaseName = mongoUrl.DatabaseName;

                    // Create a MongoDB client and access the specified database
                    var client = new MongoClient(connectionString);
                    return client.GetDatabase(databaseName);
                });

                services.AddTransient<HttpLoggingHandler>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<HttpLoggingHandler>>();
                    var repository = sp.GetRequiredService<IRepository<HttpLogRecord>>();
                    return new HttpLoggingHandler(logger, repository, Constants.TG_BOT_HTTP_CLIENT_NAME);
                });

                services.AddHttpClient(Constants.TG_BOT_HTTP_CLIENT_NAME)
                    .AddHttpMessageHandler<HttpLoggingHandler>();

                services.AddHostedService<BotService>();
                services.AddSingleton<ITelegramBotClient>(provider =>
                {
                    var botToken = configuration[Constants.TG_BOT_TOKEN_SETTING_NAME];
                    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient(Constants.TG_BOT_HTTP_CLIENT_NAME);
                    return new TelegramBotClient(new TelegramBotClientOptions(botToken), httpClient);
                });

                // Register HttpTrackingRepository with collection name
                //services.AddTransient<HttpTrackingRepository>(provider =>
                //{
                //    var database = provider.GetRequiredService<IMongoDatabase>();
                //    var botHttpLogsCollectionName = configuration[Constants.BOT_HTTP_LOGS_COLLECTION_NAME_SETTING_NAME];
                //    return new HttpTrackingRepository(database, botHttpLogsCollectionName);
                //});

                //services.AddTransient<IRepository<HttpLogRecord>, HttpTrackingRepository>();

                services.AddTransient<IRepository<HttpLogRecord>, HttpTrackingRepository>(provider =>
                        {
                            var database = provider.GetRequiredService<IMongoDatabase>();
                            var botHttpLogsCollectionName = configuration[Constants.BOT_HTTP_LOGS_COLLECTION_NAME_SETTING_NAME];
                            return new HttpTrackingRepository(database, botHttpLogsCollectionName);
                        });
            });
    }

}