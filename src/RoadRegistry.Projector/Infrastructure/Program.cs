namespace RoadRegistry.Projector.Infrastructure;

using Be.Vlaanderen.Basisregisters.Api;
using Be.Vlaanderen.Basisregisters.Aws.DistributedMutex;
using Microsoft.AspNetCore.Hosting;

public class Program
{
    protected Program()
    { }

    public static void Main(string[] args)
    {
        Run(new ProgramOptions
        {
            Hosting =
            {
                HttpPort = 10006
            },
            Logging =
            {
                WriteTextToConsole = false,
                WriteJsonToConsole = false
            },
            Runtime =
            {
                CommandLineArgs = args
            },
            MiddlewareHooks =
            {
                ConfigureDistributedLock = DistributedLockOptions.LoadFromConfiguration
            }
        });
    }

    private static void Run(ProgramOptions options)
    {
        new WebHostBuilder()
            .UseDefaultForApi<Startup>(options)
            .RunWithLock<Program>();
    }
}
