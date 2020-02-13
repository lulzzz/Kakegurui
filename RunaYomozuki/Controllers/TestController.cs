using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MomobamiKirari.Monitor;
using MomobamiRirika.Monitor;

namespace RunaYomozuki.Controllers
{
    [Produces("application/json")]
    [ApiController]
    public class TestController : ControllerBase
    {
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
