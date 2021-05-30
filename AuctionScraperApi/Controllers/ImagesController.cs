using AuctionTigerScraper;
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
        private readonly AuctionTigerScraper.AuctionTigerScraper _auctionscraper;
        public ImagesController(AuctionTigerScraper.AuctionTigerScraper auctionScraper)
        {
            _auctionscraper = auctionScraper;
        }
        // GET api/<ImagesController>/5
        [HttpGet("{vehicleId}/{id}")]
        public async Task<IActionResult> GetImage(Guid vehicleId, int id)
        {
            var vehicle = _auctionscraper.Vehicles.Find(x => x.Id == vehicleId);
            var Picture = vehicle.GetPicture(id);
            if(Picture is null)
            {
                await _auctionscraper.DownloadPicturesAsync(new Vehicle[] { vehicle });
                if (vehicle.GetPicture(id) is null)
                    return NotFound();
                Picture = vehicle.GetPicture(id);
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
