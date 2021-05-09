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
        private readonly AuctionScraper _auctionScraper;
        public VehicleController(AuctionScraper auctionScraper)
        {
            _auctionScraper = auctionScraper;
        }
        // GET: api/<ValuesController>
        [HttpGet]
        public ActionResult<IEnumerable<Vehicle>> GetAll()
        {
            List<Vehicle> vehicles = new List<Vehicle>();
            foreach(var auction in _auctionScraper.Auctions)
                foreach(var vehicle in auction.Vehicles)
                {
                    vehicles.Add(vehicle);
                }
            if (vehicles.Count == 0)
                return NotFound();
            return Ok(vehicles);
        }

        // GET api/<ValuesController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Vehicle>> Get(Guid id)
        {
            var vehicles = _auctionScraper.Vehicles;
            var vehicle = vehicles.Where(vehicle => vehicle.Id == id).FirstOrDefault();
            if (vehicle is null)
                return NotFound();
            return Ok(vehicle);
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<Vehicle>>> Get(IEnumerable<string> vehicle_names)
        {
            IEnumerable<Vehicle> vehicles = await _auctionScraper.GetDesirableVehiclesAsync(vehicle_names);
            if (vehicles.Count() == 0)
                return NotFound();
            return Ok(vehicles);
        }
    }
}
