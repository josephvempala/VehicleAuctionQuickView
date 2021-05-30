using AuctionTigerScraper.Database_Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace AuctionTigerScraper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<IMongoClient, MongoClient>(item => new MongoClient(hostContext.Configuration["mongoURI"]));
                    services.AddSingleton<IAuctionScraper, AuctionTigerScraper>();
                });
    }
}
