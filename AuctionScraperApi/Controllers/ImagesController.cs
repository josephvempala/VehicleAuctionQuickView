﻿using AuctionTigerScraper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Threading.Tasks;

namespace AuctionScraperApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly AuctionScraper _auctionscraper;
        public ImagesController(AuctionScraper auctionScraper)
        {
            _auctionscraper = auctionScraper;
        }
        // GET api/<ImagesController>/5
        [HttpGet("{vehicleId}/{id}")]
        public async Task<IActionResult> GetImage(Guid vehicleId, int id)
        {
            string Picture;
            try
            {
                Picture = _auctionscraper.Vehicles.Find(item => item.Id == vehicleId).GetPictureAsync(id);
            }
            catch
            {
                return NotFound();
            }
            if (Picture is null)
            {
                return NotFound();
            }
            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(Picture, out string contentType))
            {
                contentType = "application/octet-stream";
            }
            byte[] bytes = await System.IO.File.ReadAllBytesAsync(Picture);
            return File(bytes, contentType, id.ToString());
        }
    }
}
