using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace raceanalyst
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            using (var scope = host.Services.CreateScope())
            {
                LineCrossingAnalyzer lca = new LineCrossingAnalyzer(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());

                if ((args.Length == 0) || ("--manual" == args[0]))
                {
                    lca.AnalyzeFunction = lca.PublishImage;
                }
                else if ("--num" == args[0])
                {
                    lca.AnalyzeFunction = lca.AnalyzeByNumbers;
                }
                else if ("--qr" == args[0])
                {
                    lca.AnalyzeFunction = lca.AnalyzeByQRCode;
                }
                else if ("--train" == args[0])
                {
                    string trainImage = args[1];

                    Console.WriteLine($"Training image is {trainImage}");

                    DataModel.GetDataModel().Train(trainImage);
                    return;
                }
            }

            host.Run();
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
