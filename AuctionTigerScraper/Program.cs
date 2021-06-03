using AuctionTigerScraper.Database_Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System.IO;

namespace AuctionTigerScraper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var root = Directory.GetCurrentDirectory();
            var dotEnv = Path.Combine(root, ".env");
            DotEnv.Load(dotEnv);
            CreateHostBuilder(args).ConfigureAppConfiguration(
                configBuilder => 
                configBuilder.SetBasePath(root)
                .AddEnvironmentVariables()
            )
            .Build()
            .Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<IMongoClient, MongoClient>(item => new MongoClient(hostContext.Configuration["MongoURI"]));
                    services.AddSingleton<IAuctionScraper, AuctionTigerScraper>();
                });
    }
}
