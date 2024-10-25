using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Data;

public class DbInitializer
{
    public static async Task InitDb(WebApplication app) 
    {
            await DB.InitAsync(
                "SearchDb", 
                MongoClientSettings.FromConnectionString(
                    app.Configuration.GetConnectionString("MongoDbConnection")
                )
            );

            await DB.Index<Item>()
                .Key(x => x.Make, KeyType.Text)
                .Key(x => x.Model, KeyType.Text)
                .Key(x => x.Color, KeyType.Text)
                .CreateAsync();

            var count = await DB.CountAsync<Item>();

            if (count == 0) 
            {
                Console.WriteLine("Trying to seed data");

                var itemListData = File.ReadAllText("Data/auctions.json");

                JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };

                List<Item> items = JsonSerializer.Deserialize<List<Item>>(itemListData, jsonOptions);

                await DB.SaveAsync(items);
            }
    }    
}
