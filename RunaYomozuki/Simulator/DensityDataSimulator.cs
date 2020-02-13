using System;
using Kakegurui.WebExtensions;
using MomobamiRirika.Adapter;

namespace RunaYomozuki.Simulator
{
    /// <summary>
    /// smo高点密度数据模拟器
    /// </summary>
    public class DensityDataSimulator: TrafficDataSimulator
    {
        /// <summary>
        /// 随机值
        /// </summary>
        private readonly Random _random = new Random();

        /// <summary>
        /// 数据生成模式
        /// </summary>
        private readonly DataCreateMode _mode;

        public DensityDataSimulator(int channelCount,int itemCount,DataCreateMode mode)
            : base("/high_point",333, channelCount, itemCount,0)
        {
            _mode = mode;
        }

        protected override void SendDataCore(string url,int channelCount,int itemCount)
        {
            DateTime now = DateTime.Now;
            for (int i=1;i<= channelCount; ++i)
            {
                for (int j = 1; j <= itemCount; ++j)
                {
                    int value = _random.Next(1, 300);
                    DensityAdapterData density = new DensityAdapterData
                    {
                        data = new DensityData
                        {
                            channel_id = i,
                            region_id = j,
                            record_time = now.ToString("yyyyMMddHHmmss")
                        },
                        type = "car_count"
                    };
                    if (_mode == DataCreateMode.Random)
                    {
                        density.data.count = value;
                    }
                    else if(_mode==DataCreateMode.Sequence)
                    {
                        density.data.count = now.Minute;
                    }
                    else
                    {
                        density.data.count = 1;
                    }
                   
                    WebSocketMiddleware.Broadcast(url, density);
                    if (value==1)
                    {
                        DensityAdapterData eventData = new DensityAdapterData
                        {
                            data = new DensityData
                            {
                                channel_id = i,
                                region_id = j,
                                record_time = density.data.record_time
                            },
                            type = "crowd_data"
                        };
                        WebSocketMiddleware.Broadcast(url, eventData);
                    }
                }
            }
     
        }
    }
}
