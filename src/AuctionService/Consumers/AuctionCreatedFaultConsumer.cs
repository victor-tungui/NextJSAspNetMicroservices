using System;
using Contracts;
using MassTransit;

namespace AuctionService.Consumers;

public class AuctionCreatedFaultConsumer : IConsumer<Fault<AuctionCreated>>
{
    public async Task Consume(ConsumeContext<Fault<AuctionCreated>> context)
    {
        Console.WriteLine("--> Consuming faulty created");

        ExceptionInfo exception = context.Message.Exceptions.First();

        Console.WriteLine($"*** Argument Text: {typeof(System.ArgumentException).ToString()}");
        if (exception.ExceptionType == "System.ArgumentException")
        {
            context.Message.Message.Model = "FooBar";

            await context.Publish(context.Message.Message);
        } else
        {
            Console.WriteLine("Not an argument exceptio - update error dashboard somewhere");
        }
    }
}
