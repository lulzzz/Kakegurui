using System;
using Kakegurui.Log;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RunaYomozuki.Simulator;

namespace RunaYomozuki
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            IConfiguration configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            int listenPort = configuration.GetValue<int>("ListenPort");
            int channelCount = configuration.GetValue<int>("ChannelCount");
            int itemCount = configuration.GetValue<int>("ItemCount");
            int channelId = configuration.GetValue<int>("ChannelId");
            int model = configuration.GetValue<int>("Mode");
            string url = configuration.GetValue<string>("Url");
      
            int flow = configuration.GetValue<int>("Flow");
            if (flow == 0)
            {
                FlowDataSimulator flowSimulator = new FlowDataSimulator(channelCount, itemCount, channelId,(DataCreateMode)model);
                flowSimulator.Start();
            }

            int video = configuration.GetValue<int>("Video");
            if (video == 0)
            {
                VideoStructDataSimulator videoSimulator = new VideoStructDataSimulator(channelCount, itemCount, channelId);
                videoSimulator.Start();
            }

            int io = configuration.GetValue<int>("IO");
            if (io == 0)
            {
                IODataSimulator ioSimulator = new IODataSimulator(channelCount, itemCount, channelId);
                ioSimulator.Start();
            }

            int density = configuration.GetValue<int>("Density");
            if (density == 0)
            {
                DensityDataSimulator densitySimulator = new DensityDataSimulator(channelCount, itemCount, (DataCreateMode)model);
                densitySimulator.Start();
            }

            int violation = configuration.GetValue<int>("Violation");
            if (violation == 0)
            {
                ViolationDataSimulator violationDataSimulator = new ViolationDataSimulator(url);
                violationDataSimulator.Start();
            }

            IWebHost webHost = WebHost.CreateDefaultBuilder(args)
                .UseUrls($"http://+:{listenPort}/")
                .UseStartup<Startup>()
                .Build();

            Console.WriteLine($"ListenPort {listenPort}");
            Console.WriteLine($"ChannelCount {channelCount}");
            Console.WriteLine($"ItemCount {itemCount}");
            webHost.Run();
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            LogPool.Logger.LogError((int)LogEvent.系统,ex, $"Runtime terminating: {e.IsTerminating}");
        }
    }
}
