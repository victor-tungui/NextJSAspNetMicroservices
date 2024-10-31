using AuctionService.Consumers;
using AuctionService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionDbContext>(options => {
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddMassTransit(m => 
{
    m.AddEntityFrameworkOutbox<AuctionDbContext>(options => {
        options.QueryDelay = TimeSpan.FromSeconds(10);
        options.UsePostgres();
        options.UseBusOutbox();
    });

    m.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>();
    m.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));

    m.UsingRabbitMq((context, config) => 
    {
        config.ConfigureEndpoints(context);
    });
});

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseAuthorization();
app.MapControllers();

try 
{
    DbInitializer.InitDb(app);
} catch(Exception ex) {
    Console.WriteLine(ex);
}

app.Run();
