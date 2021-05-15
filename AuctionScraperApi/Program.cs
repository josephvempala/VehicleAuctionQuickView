using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace AuctionScraperApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://192.168.0.61:5001", "http://192.168.192.179:5001");
                });
    }
}
