using SearchService.Models;

namespace SearchService.DTOs;

public record class ItemResultsDto (List<Item> Results, int PageCount, long TotalCount) { }

