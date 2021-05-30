using AuctionScraperApi.Configuration;
using AuctionTigerScraper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace AuctionScraperApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            UserLogin loginDetails = Configuration.GetSection(nameof(UserLogin)).Get<UserLogin>();
            ScraperPrecaching Precaching = Configuration.GetSection(nameof(ScraperPrecaching)).Get<ScraperPrecaching>();
            services.AddSingleton(provider =>
            {
                AuctionTigerScraper.AuctionTigerScraper auctionScraper = new AuctionTigerScraper.AuctionTigerScraper();
                auctionScraper.InitializeScraperAsync(new ScraperOptions { Username = loginDetails.Username, Password = loginDetails.Password, DesirableVehicles = Precaching.DesirableVehicles }).Wait();
                return auctionScraper;
            });
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuctionScraperApi", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuctionScraperApi v1"));
            }
            app.UseCors(options => options.AllowAnyOrigin());
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
