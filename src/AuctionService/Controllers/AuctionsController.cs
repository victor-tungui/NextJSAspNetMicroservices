
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuctionsController: ControllerBase 
{
    private readonly AuctionDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndPoint;

    public AuctionsController(
        AuctionDbContext context, 
        IMapper mapper, 
        IPublishEndpoint publishEndPoint)
    {
        _context = context;
        _mapper = mapper;
        _publishEndPoint = publishEndPoint;
    }

    [HttpGet]
    public async Task<Results<Ok<List<AuctionDto>>,BadRequest>> GetAllActions(string date)
    {
        var query = _context.Auctions.OrderBy(a => a.Item.Make).AsQueryable();

        if (!string.IsNullOrWhiteSpace(date))
        {
            DateTime dateToCompare = DateTime.Parse(date).ToUniversalTime();

            query.Where(a => a.UpdatedAt.CompareTo(dateToCompare) > 0);
        }

        var auctions = await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();

        //var auctions = await _context.Auctions
        //    .Include(a => a.Item)
        //    .OrderBy(a => a.Item.Make)
        //    .ToListAsync();
        
        //var auctionsDtoList = _mapper.Map<List<AuctionDto>>(auctions);

        return TypedResults.Ok(auctions);
    }

    [HttpGet("{id}")]
    public async Task<Results<Ok<AuctionDto>,NotFound>> GetAuctionById(Guid id)
    {
        var auction = await _context.Auctions
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (auction == null)
        {
            return TypedResults.NotFound();
        }

        var dto = _mapper.Map<AuctionDto>(auction);

        return TypedResults.Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
    {
        var auction = _mapper.Map<Auction>(auctionDto);
        //TODO: Add user as seller
        auction.Seller = "Saul";

        _context.Auctions.Add(auction);

        var newAuction = _mapper.Map<AuctionDto>(auction);
        
        await _publishEndPoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

        var result = await _context.SaveChangesAsync() > 0;

        if (!result) 
        {
            return BadRequest("Could not save changes to the database");
        }

        return CreatedAtAction(
            nameof(GetAuctionById),
            new{auction.Id},
            newAuction
        );
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
    {
        var auction = await _context.Auctions.Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (auction is null) 
        {
            return NotFound();
        }

        // TODO: Check seller is the same username
        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

        await _publishEndPoint.Publish(_mapper.Map<AuctionUpdated>(auction));

        var result = await _context.SaveChangesAsync() > 0;

        if (result) 
        {
            return Ok();
        }

        return BadRequest("Unable to update Auction");
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await _context.Auctions.FindAsync(id);

        if (auction is null) 
        {
            return NotFound();
        }

        // TODO: Validate user is seller
        _context.Auctions.Remove(auction);

        await _publishEndPoint.Publish<AuctionDeleted>(new { Id = auction.Id.ToString() });

        var result = await _context.SaveChangesAsync() > 0;

        if (!result)
        {
            return BadRequest("Unable to delete the auction");
        }

        return NoContent();
    }
}