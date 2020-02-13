using System;
using Kakegurui.Core;
using Kakegurui.WebExtensions;

namespace RunaYomozuki.Simulator
{
    public class IODataSimulator:TrafficDataSimulator
    {
        private readonly Random _random = new Random();
        public IODataSimulator(int channelCount, int itemCount,int channelId)
            : base("/sub/deviceio", 3000, channelCount, itemCount, channelId)
        {
        }

        protected override void SendDataCore(string url,int channelCount,int itemCount)
        {
            int channelId = _channelId;
            for(int i=1;i<= channelCount; ++i)
            {
                for(int j=1;j<= itemCount; ++j)
                {
                    IOData data = new IOData
                    {
                        ChannelId = $"channel_{channelId}",
                        LaneId = $"{j:D2}",
                        Timestamp = TimeStampConvert.ToUtcTimeStamp(),
                        Status = _random.Next(1, 100) % 2 == 1 ? 1 : 0
                    };
                    WebSocketMiddleware.Broadcast(url, data);
                }

                channelId += 1;
            }
    
        }
    }

    class IOData
    {
        public long Timestamp { get; set; }
        public string ChannelId { get; set; }
        public string LaneId { get; set; }
        public int Status { get; set; }
    }

}
