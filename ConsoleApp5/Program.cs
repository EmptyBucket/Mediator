// See https://aka.ms/new-console-template for more information

using EasyNetQ;

var bus = RabbitHutch.CreateBus("host=localhost");
var advancedBus = bus.Advanced;

var task = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(1_000);
    }
});


await task;

namespace ConsoleApp5
{
    public record Event(string Name);
}