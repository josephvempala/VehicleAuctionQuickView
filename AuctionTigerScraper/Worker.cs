using AuctionTigerScraper.Database_Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuctionTigerScraper
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMongoCollection<VehicleModel> _vehiclesCollection;
        private readonly IConfiguration _configuration;
        private readonly IAuctionScraper _scraper;
        private bool isFirstUpdate;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IMongoClient mongoClient, IAuctionScraper auctionScraper)
        {
            _logger = logger;
            _vehiclesCollection = mongoClient.GetDatabase("auctionScraper").GetCollection<VehicleModel>("vehicles");
            _configuration = configuration;
            _scraper = auctionScraper;
            isFirstUpdate = true;
        }

        public async void UpdateDatabase(Vehicle[] newVehicles, Vehicle[] expiredVehicles)
        {
            if (isFirstUpdate)
            {
                var registrationNumbers = newVehicles.Select(item => item.RegistrationNumber.ToString());
                await _vehiclesCollection.UpdateManyAsync(
                    Builders<VehicleModel>.Filter.And(
                        Builders<VehicleModel>.Filter.Eq(item => item.isActive, true),
                        Builders<VehicleModel>.Filter.Not(
                            Builders<VehicleModel>.Filter.In(item => item.RegistrationNumber, registrationNumbers)
                        )
                    ),
                    Builders<VehicleModel>.Update.Set(item => item.isActive, false)
                );
                var existingVehicles = await _vehiclesCollection.Find(Builders<VehicleModel>.Filter.In(item => item.RegistrationNumber, registrationNumbers)).Project(item => item.RegistrationNumber).ToListAsync();
                newVehicles = newVehicles.Where(item => !existingVehicles.Exists(x => x == item.RegistrationNumber.ToString())).ToArray();
                isFirstUpdate = false;
            }
            if(newVehicles.Length > 0)
            {
                List<WriteModel<VehicleModel>> vehiclesbulk = new List<WriteModel<VehicleModel>>();
                foreach (Vehicle vehicle in newVehicles)
                {
                    vehiclesbulk.Add(new InsertOneModel<VehicleModel>(vehicle.AsDatabaseModel() with { isActive = true }));
                }
                BulkWriteResult<VehicleModel> result = await _vehiclesCollection.BulkWriteAsync(vehiclesbulk);
            }
            if (expiredVehicles.Length > 0)
            {
                List<WriteModel<VehicleModel>> vehiclesbulk = new List<WriteModel<VehicleModel>>();
                foreach (Vehicle vehicle in expiredVehicles)
                {
                    vehiclesbulk.Add(new UpdateOneModel<VehicleModel>(Builders<VehicleModel>.Filter.Eq(item => item.Reference, vehicle.Reference), Builders<VehicleModel>.Update.Set(item=> item.isActive, false)));
                }
                BulkWriteResult<VehicleModel> result = await _vehiclesCollection.BulkWriteAsync(vehiclesbulk);
            }
            _logger.LogInformation(DateTime.Now.ToString() + " Successfully updated changes");
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            ScraperOptions scraperOptions = new ScraperOptions() {
                Username = _configuration["ScraperOptions_Username"],
                Password = _configuration["ScraperOptions_Password"]
            };
            _scraper.ListenForChanges(UpdateDatabase);
            try
            {
                await _scraper.InitializeScraperAsync(scraperOptions, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
            _logger.LogInformation(DateTime.Now.ToString() + " Service Started");
            await ExecuteAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _scraper.CheckForChangesAsync(stoppingToken);
                    _logger.LogInformation(DateTime.Now.ToString() + " Checked for changes");
                }
                catch (Exception e)
                {
                    _logger.LogError(DateTime.Now.ToString()+ e.Message);
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromMinutes(double.Parse(_configuration.GetSection("RecheckFrequency").Value)), stoppingToken);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _scraper.DisposeAsync();
            _logger.LogInformation(DateTime.Now.ToString() + " Shutting Down");
        }
    }
}
