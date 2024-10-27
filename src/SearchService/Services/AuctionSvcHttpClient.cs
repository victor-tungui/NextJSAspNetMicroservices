using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services;

public class AuctionSvcHttpClient
{
	private readonly HttpClient _httpClient;
	private readonly IConfiguration _config;

	public AuctionSvcHttpClient(HttpClient httpClient, IConfiguration config)
    {
		_httpClient = httpClient;
		_config = config;
	}

	public async Task<List<Item>> GetItemsForSearchDb()
	{
		var lastUpdated = await DB.Find<Item, string>()
			.Sort(i => i.Descending(x => x.UpdatedAt))
			.Project(i => i.UpdatedAt.ToString())
			.ExecuteFirstAsync();

		string baseUrl = _config["AuctionServiceUrl"];

		List<Item> result = await _httpClient.GetFromJsonAsync<List<Item>>($"{baseUrl}/api/auctions?date={lastUpdated}");

		return result;
	}
}

