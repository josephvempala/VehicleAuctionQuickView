﻿using AuctionTigerScraper;
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
        public ActionResult<IEnumerable<Vehicle>> GetAll(int index, int limit)
        {
            List<Vehicle> vehicles;
            try
            {
                 vehicles = _auctionScraper.Vehicles.GetRange(index, limit);
            }
            catch
            {
                return BadRequest("invalid index and or limit");
            }
            if (vehicles.Count == 0)
                return NotFound();
            return Ok(vehicles);
        }

        // GET api/<ValuesController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Vehicle>> Get(Guid id)
        {
            List<Vehicle> vehicles = _auctionScraper.Vehicles;
            Vehicle vehicle = vehicles.Where(vehicle => vehicle.Id == id).First();
            if (vehicle is null)
                return NotFound();
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
