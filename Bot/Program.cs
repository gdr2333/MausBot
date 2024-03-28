using MausBot.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal class Program
{
    private static void Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<BotService>();

        IHost host = builder.Build();
        try
        {
            host.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}