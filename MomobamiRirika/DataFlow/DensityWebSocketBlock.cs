using ItsukiSumeragi.DataFlow;
using Kakegurui.WebExtensions;
using MomobamiRirika.Models;

namespace MomobamiRirika.DataFlow
{
    /// <summary>
    /// 密度数据websocket数据块
    /// </summary>
    public class DensityWebSocketBlock: TrafficActionBlock<TrafficDensity>
    {
        /// <summary>
        /// 密度数据推送ws地址
        /// </summary>
        public const string DensityUrl = "/websocket/density/";

        protected override void Handle(TrafficDensity data)
        {
            WebSocketMiddleware.Broadcast($"{DensityUrl}{data.DataId}", data);
        }
    }
}
