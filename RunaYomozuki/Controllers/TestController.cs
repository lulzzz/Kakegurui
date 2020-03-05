using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace RunaYomozuki.Controllers
{
    [Produces("application/json")]
    [ApiController]
    public class TestController : ControllerBase
    {
        public class DensityChannelList
        {
            public int Code { get; set; }
            public DensityChannel[] Data { get; set; }
        }

        public class DensityChannel
        {
            public int ChannelId { get; set; }
            public int Status { get; set; }
            public string RtmpUrl { get; set; }
        }
        public class FlowChannellistClass
        {
            public int Code { get; set; }
            public ChannelDataClass Data { get; set; }
        }

        public class ChannelDataClass
        {
            public int Totalnumber { get; set; }

            public ChannelinfolistClass[] Channelinfolist { get; set; }
        }

        public class ChannelinfolistClass
        {
            public string ChannelId { get; set; }
            public int ChannelStatus { get; set; }
        }

        public class DeviceClass
        {
            public int Code { get; set; }
            public DeviceDataClass Data { get; set; }
        }

        public class DeviceDataClass
        {
            public string Licstatus { get; set; }
            public string Space { get; set; }
            public string Systime { get; set; }
            public string Runtime { get; set; }
        }

        private readonly IConfiguration _configuration;

        public TestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
       
        [HttpGet("api/channel/list")]
        public DensityChannelList TestDensity()
        {
            int channelCount = _configuration.GetValue<int>("ChannelCount");
            DensityChannelList list = new DensityChannelList
            {
                Code = 0,
                Data = new DensityChannel[channelCount]
            };
            for (int i = 0; i < channelCount; ++i)
            {
                list.Data[i] = new DensityChannel
                {
                    ChannelId = i + 1,
                    Status = 1,
                    RtmpUrl="rtmp://58.200.131.2:1935/livetv/hunantv"
                };
            }

            return list;
        }

        [HttpGet("app/aiboxManagerAPI/config_handler/channelparams")]
        public FlowChannellistClass TestFlow()
        {
            int channelCount = _configuration.GetValue<int>("ChannelCount");
            int deviceId = _configuration.GetValue<int>("DeviceId");

            FlowChannellistClass c = new FlowChannellistClass()
            {
                Code=0,
                Data = new ChannelDataClass
                {
                    Channelinfolist = new ChannelinfolistClass[channelCount]
                }
            };
            for(int i=0;i<c.Data.Channelinfolist.Length;++i)
            {
                c.Data.Channelinfolist[i] = new ChannelinfolistClass
                {
                    ChannelStatus = 1,
                    ChannelId = $"channel_{deviceId}_{i+1}"
                };
            }

            return c;
        }

        [HttpGet("app/aiboxManagerAPI/config_handler/single_channelparam")]
        public Videoclass GetChannel()
        {
            Videoclass v = new Videoclass
            {
                Code = 0,
                Data = new Video1class
                {
                    RtspUrl = "rtmp://58.200.131.2:1935/livetv/hunantv",
                    VideoHeight = 1920,
                    VideoWidth = 1080,
                    ChannelStatis = 1
                }
            };
            return v;
        }
    }

    public class Video1class
    {
        public string RtspUrl { get; set; }
        public int VideoHeight { get; set; }
        public int VideoWidth { get; set; }
        public int ChannelStatis { get; set; }
    }

    public class Videoclass
    {
        public int Code { get; set; }
        public Video1class Data { get; set; }
    }
}
