using System;
using Kakegurui.Core;
using Kakegurui.WebExtensions;
using MomobamiKirari.Adapter;
using ItsukiSumeragi.Codes.Flow;

namespace RunaYomozuki.Simulator
{
    /// <summary>
    /// smo流量模拟器
    /// </summary>
    public class FlowDataSimulator: TrafficDataSimulator
    {
        /// <summary>
        /// 模拟数据生成方式
        /// </summary>
        private readonly DataCreateMode _mode;

        public FlowDataSimulator(int channelCount, int itemCount, int channelId, DataCreateMode mode)
            : base("/sub/crossingflow",60*1000, channelCount, itemCount, channelId)
        {
            _mode = mode;
        }

        protected override void SendDataCore(string url,int channelCount,int itemCount)
        {
            DateTime now = DateTime.Now;
            long timestamp =
                TimeStampConvert.ToUtcTimeStamp(new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0));
            Random random = new Random();

            int channelId = _channelId;
            for(int i=1;i<= channelCount;++i)
            {
                FlowAdapterData flow = new FlowAdapterData
                {
                    Data = new LaneAdapterData[itemCount]
                };
                for (int j=1;j<=itemCount;++j)
                {
                    if (_mode == DataCreateMode.Random)
                    {
                        flow.Data[j - 1] = new LaneAdapterData
                        {
                            ChannelId = $"channel_{channelId}",
                            LaneId = $"{j:D2}",
                            Timestamp = timestamp,
                            Bikes = random.Next(1, 10),
                            Buss = random.Next(1, 10),
                            Cars = random.Next(1, 10),
                            Motorcycles = random.Next(1, 10),
                            Persons = random.Next(1, 10),
                            Tricycles = random.Next(1, 10),
                            Trucks = random.Next(1, 10),
                            Vans = random.Next(1, 10),
                            AverageSpeed = random.Next(1, 10),
                            HeadSpace = random.Next(1, 10),
                            HeadDistance = random.Next(1, 10),
                            Occupancy = random.Next(1, 100),
                            TimeOccupancy = random.Next(1, 100),
                            TrafficStatus = (TrafficStatus)random.Next(1, 5)
                        };
                    }
                    else if(_mode==DataCreateMode.Sequence)
                    {
                        flow.Data[j - 1] = new LaneAdapterData
                        {
                            ChannelId = $"channel_{channelId}",
                            LaneId = $"{j:D2}",
                            Timestamp = timestamp,
                            Bikes = now.Minute,
                            Buss = now.Minute,
                            Cars = now.Minute,
                            Motorcycles = now.Minute,
                            Persons = now.Minute,
                            Tricycles = now.Minute,
                            Trucks = now.Minute,
                            Vans = now.Minute,
                            AverageSpeed = now.Minute,
                            HeadSpace = now.Minute,
                            HeadDistance = now.Minute,
                            Occupancy = now.Minute,
                            TimeOccupancy = now.Minute,
                            TrafficStatus = TrafficStatus.通畅
                        };
                    }
                    else
                    {
                        flow.Data[j - 1] = new LaneAdapterData
                        {
                            ChannelId = $"channel_{channelId}",
                            LaneId = $"{j:D2}",
                            Timestamp = timestamp,
                            Bikes = 1,
                            Buss = 1,
                            Cars = 1,
                            Motorcycles = 1,
                            Persons = 1,
                            Tricycles = 1,
                            Trucks = 1,
                            Vans = 1,
                            AverageSpeed = 1,
                            HeadSpace = 1,
                            HeadDistance = 1,
                            Occupancy = 1,
                            TimeOccupancy = 1,
                            TrafficStatus = TrafficStatus.通畅
                        };
                    }

                }
                WebSocketMiddleware.Broadcast(url, flow);

                channelId += 1;
            }
        }
    }
}
