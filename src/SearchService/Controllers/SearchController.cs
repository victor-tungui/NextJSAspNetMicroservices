using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService.DTOs;
using SearchService.Models;
using SearchService.RequestHelpers;

namespace SearchService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        public SearchController() { }

        [HttpGet]
        public async Task<ActionResult<ItemResultsDto>> SearchItems ([FromQuery] SearchParams searchParams)
        {
            var query = DB.PagedSearch<Item, Item>();
            
            if (!string.IsNullOrWhiteSpace(searchParams.SearchTerm)) 
            {
                query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
            }

            query = searchParams.OrderBy switch
            {
                "make" => query.Sort(i => i.Ascending(a => a.Make)),
                "new" => query.Sort(i => i.Descending(a => a.CreatedAt)),
                _ => query.Sort(i => i.Ascending(a => a.AuctionEnd))
            };


            query = searchParams.FilterBy switch
            {
                "finished" => query.Match(i => i.AuctionEnd < DateTime.UtcNow),
                "endingSoon" => query.Match(i => i.AuctionEnd < DateTime.UtcNow.AddHours(6) && i.AuctionEnd > DateTime.UtcNow),
                _ => query.Match(i => i.AuctionEnd > DateTime.UtcNow)
            };

            if (!string.IsNullOrWhiteSpace(searchParams.Seller))
            {
                query.Match(i => i.Seller == searchParams.Seller);
            }

            if (!string.IsNullOrWhiteSpace(searchParams.Winner))
            {
                query.Match(i => i.Winner == searchParams.Winner);
            }

            query.PageNumber(searchParams.PageNumber);
            query.PageSize(searchParams.PageSize);

            var result = await query.ExecuteAsync();

            ItemResultsDto model = new([.. result.Results], result.PageCount, result.TotalCount);

            return model;
        }
    }
}
