using MassTransit;
using Contracts;
using AuctionService.Data;

namespace AuctionService.Consumers
{
    public class AuctionBidPlacedConsumer : IConsumer<BidPlaced>
    {
        private readonly AuctionDbContext _dbContext;

        public AuctionBidPlacedConsumer(AuctionDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Consume(ConsumeContext<BidPlaced> context)
        {
            Console.WriteLine($"Bid placed: {context.Message.Amount} by {context.Message.Bidder}");

            var auction = await _dbContext.Auctions.FindAsync(Guid.Parse(context.Message.AuctionId));

            if (auction.CurrentHighestBid == null
                || context.Message.BidStatus.Contains("Accepted")
                && context.Message.Amount > auction.CurrentHighestBid)
            {
                auction.CurrentHighestBid = context.Message.Amount;
                await _dbContext.SaveChangesAsync();
            }

        }
    }
}
