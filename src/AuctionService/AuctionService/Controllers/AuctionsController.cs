using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionsController : ControllerBase
    {
        private readonly AuctionDbContext _content;
        private readonly IMapper _mapper;

        public AuctionsController(AuctionDbContext context, IMapper mapper)
        {
            _content = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions()
        {
            var auctions = await _content.Auctions
                .Include(x => x.Item)
                .OrderBy(x => x.Item.Make)
                .ToListAsync();

            return Ok(_mapper.Map<List<AuctionDto>>(auctions));
        }

        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuctionsAfterDate(string date)
        {
            var query = _content.Auctions
                .OrderBy(x => x.Item.Make)
                .AsQueryable();

            if (!string.IsNullOrEmpty(date))
            {
                query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date)
                    .ToUniversalTime()) > 0);
            }

           return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
        {
            var auction = await _content.Auctions
                .Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (auction == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<AuctionDto>(auction));
        }

        [HttpPost]
        public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto createAuctionDto)
        {
            var auction = _mapper.Map<Auction>(createAuctionDto);

            _content.Auctions.Add(auction);
            var result = await _content.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Failed to create auction");

            return CreatedAtAction(nameof(GetAuctionById), new { id = auction.Id }, _mapper.Map<AuctionDto>(auction));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
        {
            var auction = await _content.Auctions
                .Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (auction == null) return NotFound();

            //TODO: check seller id == user id

            auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
            auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
            auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;
            auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
            auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;

            var result = await _content.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Failed to update auction");

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuction(Guid id)
        {
            var auction = await _content.Auctions.FirstOrDefaultAsync(x => x.Id == id);

            if (auction == null) return NotFound();

            _content.Auctions.Remove(auction);
            
            var result = await _content.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Failed to delete auction");
            
            return NoContent();
        }
    }
}
