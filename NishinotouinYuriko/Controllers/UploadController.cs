using System;
using System.Globalization;
using ItsukiSumeragi.Codes.Flow;
using ItsukiSumeragi.Codes.Violation;
using ItsukiSumeragi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NishinotouinYuriko.Cache;
using NishinotouinYuriko.DataFlow;
using NishinotouinYuriko.Models;

namespace NishinotouinYuriko.Controllers
{
    [Produces("application/json")]
    public class UploadController:Controller
    {
        private readonly ViolationBranchBlock _branchBlock;

        private readonly IMemoryCache _memoryCache;

        public UploadController(ViolationBranchBlock branchBlock,IMemoryCache memoryCache)
        {
            _branchBlock = branchBlock;
            _memoryCache = memoryCache;
        }

        [HttpPost("upload")]
        public IActionResult Upload([FromBody]ViolationData data)
        {
            TrafficChannel channel = _memoryCache.GetViolationChannel(data.Raw_data.Device_id);
            int locationId = 0;
            if (channel?.LocationId != null)
            {
                locationId = channel.LocationId.Value;
            }
            DateTime time = DateTime.ParseExact(data.Raw_data.Violate_time, "yyyy-MM-dd HH:mm:ss.fff",
                CultureInfo.CurrentCulture);

            int carType = data.Raw_data.Vehicle_type;
            int targetType;
            if (carType == (int) CarType.大型客车
                || carType == (int) CarType.大型货车
                || carType == (int) CarType.挂车
                || carType == (int) CarType.混凝土搅拌车
                || carType == (int) CarType.随车吊
                || carType == (int) CarType.渣土车
                || carType == (int) CarType.轻卡
                || carType == (int) CarType.危化品车
            )
            {
                targetType = (int) TargetType.大型车;
            }
            else if (carType == (int)CarType.二轮车
                     || carType == (int)CarType.三轮车
                     || carType == (int)CarType.自行车)
            {
                targetType = (int)TargetType.非机动车;
            }
            else if(carType==0)
            {
                targetType = (int) TargetType.未知;
            }
            else
            {
                targetType = (int)TargetType.小型车;
            }
            _branchBlock.Post(new ViolationStruct
            {
                DataId = data.Raw_data.Device_id,
                DateTime = time,
                ViolationId = data.Raw_data.Violate_type,
                CarType = carType,
                TargetType = targetType,
                PlateNumber = data.Raw_data.Plate_licence,
                LocationId = locationId,
                Direction = 1,
                Image1 = data.Images.Length >= 1 ? data.Images[0].Content : null,
                Image2 = data.Images.Length >= 2 ? data.Images[1].Content : null,
                Image3 = data.Images.Length >= 3 ? data.Images[2].Content : null,
                Image4 = data.Images.Length >= 4 ? data.Images[3].Content : null,
                Image5 = data.Images.Length >= 5 ? data.Images[4].Content : null,
                Video = data.Images.Length >= 6 ? data.Images[5].Content : null,
                ImageLink1 = data.Images.Length >= 1 ? $"/{ViolationStartup.FileRequestPath}/{time:yyyy-MM}/{data.Raw_data.Device_id}_{time:yyyyMMddHHmmssfff}_1.jpg" : null,
                ImageLink2 = data.Images.Length >= 2 ? $"/{ViolationStartup.FileRequestPath}/{time:yyyy-MM}/{data.Raw_data.Device_id}_{time:yyyyMMddHHmmssfff}_2.jpg" : null,
                ImageLink3 = data.Images.Length >= 3 ? $"/{ViolationStartup.FileRequestPath}/{time:yyyy-MM}/{data.Raw_data.Device_id}_{time:yyyyMMddHHmmssfff}_3.jpg" : null,
                ImageLink4 = data.Images.Length >= 4 ? $"/{ViolationStartup.FileRequestPath}/{time:yyyy-MM}/{data.Raw_data.Device_id}_{time:yyyyMMddHHmmssfff}_4.jpg" : null,
                ImageLink5 = data.Images.Length >= 5 ? $"/{ViolationStartup.FileRequestPath}/{time:yyyy-MM}/{data.Raw_data.Device_id}_{time:yyyyMMddHHmmssfff}_5.jpg" : null,
                VideoLink = data.Images.Length >= 6 ? $"/{ViolationStartup.FileRequestPath}/{time:yyyy-MM}/{data.Raw_data.Device_id}_{time:yyyyMMddHHmmssfff}_1.mp4" : null,
            });
            return Ok(new
            {
                ErrCode = 0
            });
        }

        public class ViolationData
        {
            public DataRow Raw_data { get; set; }
            public ImageData[] Images { get; set; }
        }

        public class DataRow
        {
            public string Device_id { get; set; }
            public string Violate_time { get; set; }
            public int Violate_type { get; set; }
            public string Violate_place { get; set; }
            public int Vehicle_type { get; set; }
            public string Plate_licence { get; set; }
        }

        public class ImageData
        {
            public string Name { get; set; }
            public string Content { get; set; }
        }
    }
}
