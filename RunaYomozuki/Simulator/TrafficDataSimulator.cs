using System.Threading;
using Kakegurui.Core;
using Kakegurui.WebExtensions;

namespace RunaYomozuki.Simulator
{
    /// <summary>
    /// 模拟数据生成方式
    /// </summary>
    public enum DataCreateMode
    {
        Fixed,
        Sequence,
        Random
    }

    /// <summary>
    /// smo数据模拟器
    /// </summary>
    public abstract class TrafficDataSimulator:TaskObject
    {
        /// <summary>
        /// ws地址
        /// </summary>
        private readonly string _url;

        /// <summary>
        /// 发送间隔时间(毫秒)
        /// </summary>
        private readonly int _milliseconds;

        /// <summary>
        /// 通道数量
        /// </summary>
        private readonly int _channelCount;

        /// <summary>
        /// 车道或区域数量
        /// </summary>
        private readonly int _itemCount;

        /// <summary>
        /// 设备编号
        /// </summary>
        protected int _channelId;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="url">ws地址</param>
        /// <param name="milliseconds">发送间隔时间(毫秒)</param>
        /// <param name="channelCount">通道数量</param>
        /// <param name="itemCount">车道数量</param>
        /// <param name="channelId">设备编号</param>
        protected TrafficDataSimulator(string url,int milliseconds,int channelCount,int itemCount,int channelId)
            :base("simulator")
        {
            _url = url;
            _milliseconds = milliseconds;
            _channelCount = channelCount;
            _itemCount = itemCount;
            _channelId = channelId;
        }

        protected override void ActionCore()
        {
            WebSocketMiddleware.AddUrl(_url);
            while (!IsCancelled())
            {
                SendDataCore(_url,_channelCount, _itemCount);
                Thread.Sleep(_milliseconds);
            }

            WebSocketMiddleware.RemoveUrl(_url);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="url">ws url</param>
        /// <param name="channelIndex">通道序号</param>
        /// <param name="itemIndex">车道或区域序号</param>
        protected abstract void SendDataCore(string url,int channelIndex,int itemIndex);

    }
}
