using ItsukiSumeragi.Cache;
using Microsoft.Extensions.Caching.Memory;
using MomobamiKirari.Codes;
using MomobamiKirari.Models;

namespace MomobamiKirari.Cache
{
    /// <summary>
    /// 视频结构化数据缓存
    /// </summary>
    public static class VideoCache
    {
        /// <summary>
        /// 填充视频结构化缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="videoStruct">视频结构化</param>
        /// <returns>视频结构化</returns>
        public static VideoStruct FillVideo(this IMemoryCache memoryCache, VideoStruct videoStruct)
        {
            if (videoStruct != null)
            {
                Lane lane = memoryCache.GetLane(videoStruct.DataId);
                if (lane != null)
                {
                    videoStruct.LaneName = lane.LaneName;
                    videoStruct.ChannelName = lane.Channel.ChannelName;
                    videoStruct.Direction = lane.Direction;
                    videoStruct.Direction_Desc = memoryCache.GetCode(typeof(LaneDirection), videoStruct.Direction);
                }

                if (videoStruct is VideoVehicle vehicle)
                {
                    vehicle.CarType_Desc = memoryCache.GetCode(typeof(CarType), vehicle.CarType);
                    vehicle.CarColor_Desc = memoryCache.GetCode(typeof(CarColor), vehicle.CarColor);
                    vehicle.PlateType_Desc = memoryCache.GetCode(typeof(PlateType), vehicle.PlateType);
                }
                else if (videoStruct is VideoBike bike)
                {
                    bike.BikeType_Desc = memoryCache.GetCode(typeof(NonVehicle), bike.BikeType);
                }
                else if (videoStruct is VideoPedestrain pedestrain)
                {
                    pedestrain.Age_Desc = memoryCache.GetCode(typeof(Age), pedestrain.Age);
                    pedestrain.Sex_Desc = memoryCache.GetCode(typeof(Sex), pedestrain.Sex);
                    pedestrain.UpperColor_Desc = memoryCache.GetCode(typeof(UpperColor), pedestrain.UpperColor);
                }
            }
            return videoStruct;
        }
    }
}
