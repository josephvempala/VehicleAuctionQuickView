using AuctionTigerScraper;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AuctionScraperApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleController : ControllerBase
    {
        private readonly AuctionTigerScraper.AuctionTigerScraper _auctionScraper;
        public VehicleController(AuctionTigerScraper.AuctionTigerScraper auctionScraper)
        {
            _auctionScraper = auctionScraper;
        }
        // GET: api/<ValuesController>
        [HttpGet]
        public ActionResult<IEnumerable<Vehicle>> GetAll(int index, int limit)
        {
            if (index < 0 || index > _auctionScraper.Vehicles.Count)
                return BadRequest();
            if(index+limit > _auctionScraper.Vehicles.Count)
                return Ok(_auctionScraper.Vehicles.GetRange(index, _auctionScraper.Vehicles.Count - index).Select(item => new { item.Id, item.Name, item.Year, item.Fuel, item.RegistrationNumber } ));
            return Ok(_auctionScraper.Vehicles.GetRange(index, limit).Select(item => new { item.Id, item.Name, item.Year, item.Fuel, item.RegistrationNumber }));
        }

        // GET api/<ValuesController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Vehicle>> Get(Guid id)
        {
            Vehicle vehicle = _auctionScraper.Vehicles.Where(vehicle => vehicle.Id == id).First();
            if (vehicle is null)
                return NotFound();
            await _auctionScraper.DownloadPicturesAsync(new Vehicle[] { vehicle });
            return Ok(vehicle);
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<Vehicle>>> Get(IEnumerable<string> vehicle_names)
        {
            IEnumerable<Vehicle> vehicles = await _auctionScraper.GetDesirableVehiclesAsync(vehicle_names);
            if (vehicles.Count() == 0 || vehicles is null)
                return NotFound();
            return Ok(vehicles);
        }
    }
}
