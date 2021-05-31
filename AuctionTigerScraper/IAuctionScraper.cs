using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuctionTigerScraper
{
    public interface IAuctionScraper
    {
        Task<bool> CheckForChangesAsync(CancellationToken? cancellationToken = null);
        ValueTask DisposeAsync();
        Task InitializeScraperAsync(ScraperOptions scraperOptions, CancellationToken? cancellationToken = null);
        void ListenForChanges(Action<Vehicle[], Vehicle[]> toBeCalledOnChanges);
        void StopListeningForChanges(Action<Vehicle[], Vehicle[]> toBeRemoved);
    }
}