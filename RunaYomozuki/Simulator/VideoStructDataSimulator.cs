﻿using System;
using Kakegurui.Core;
using Kakegurui.WebExtensions;
using MomobamiKirari.Adapter;
using ItsukiSumeragi.Codes.Flow;

namespace RunaYomozuki.Simulator
{
    /// <summary>
    /// smo视频结构化模拟器
    /// </summary>
    public class VideoStructDataSimulator : TrafficDataSimulator
    {

        public VideoStructDataSimulator(int channelCount, int itemCount, int channelId)
            : base("/sub/structall", 9 * 1000, channelCount, itemCount, channelId)
        {
        }

        protected override void SendDataCore(string url, int channelCount, int itemCount)
        {
            int channelId = _channelId;
            for (int i = 1; i <= channelCount; ++i)
            {
                for (int j = 1; j <= itemCount; ++j)
                {
                    DateTime now = DateTime.Now;
                    Random random = new Random();
                    VideoStructAdapterData bike = new VideoStructAdapterData
                    {
                        VideoStructType = VideoStructType.非机动车,
                        ChannelId = $"channel_{channelId}_{i}",
                        LaneId = $"{j:D2}",
                        Timestamp = TimeStampConvert.ToUtcTimeStamp(now),
                        Feature = "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",
                        Image = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAA0JCgsKCA0LCgsODg0PEyAVExISEyccHhcgLikxMC4p\r\nLSwzOko+MzZGNywtQFdBRkxOUlNSMj5aYVpQYEpRUk//2wBDAQ4ODhMREyYVFSZPNS01T09PT09P\r\nT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0//wAARCAAkACoDASEA\r\nAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA\r\nAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3\r\nODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm\r\np6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA\r\nAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx\r\nBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK\r\nU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3\r\nuLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDpWXAw\r\nenaq8gVepAq0SV32/wB4fnUDAdiDTGRlaNh9T+dSBvSDPTrjpTwba1toBdWJlnlUsVP8I/GmIYs+\r\nl4+bSnX6N/8AXqIrpV3J5NqlxBclSUBRsOR7nigZmKxkQMylTyCD2I60nFAjdhsIbq7Erq3mICUI\r\nJwD2JFTNbaurY/twrjpmyX5v1zUsaAwa4Rxq0eR62R5+tQuPEeQFvdJLg8eZE4/rQgMKK0uWv724\r\nu54PtJfE8MWdqnsR9RT/ACz6H8qoRd1bVptEEE8SQu8wYIkjY3Yx0OaxZviNerFgQWfmE4OA7bPc\r\n/wD66mwy4fHVoyIV1bD7RvH2QlQfbvUcXj2TczPp8NwycCVJNm4euG6UWGTaZcNq1tdauYxCLiXa\r\nEDZwB61YwaZJh/EuRo10srj5EcgdutS6c1hd2kHn6LphJRckQkE8A+tMZtxeF9AlSF20mAGTOQrO\r\nB/Os7xp4Z0bSfDVxc2NkscygBX3MSvPbJpAL4MHm+Cogxx++bOO/Irof7Ph/vP8AmP8ACmI//9k=",
                        BikeType = random.Next(2, 3)
                    };
                    WebSocketMiddleware.Broadcast(url, bike);

                    VideoStructAdapterData vehicle = new VideoStructAdapterData
                    {
                        VideoStructType = VideoStructType.机动车,
                        ChannelId = $"channel_{channelId}_{i}",
                        LaneId = $"{j:D2}",
                        Timestamp = TimeStampConvert.ToUtcTimeStamp(now),
                        Feature = "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",
                        Image = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAA0JCgsKCA0LCgsODg0PEyAVExISEyccHhcgLikxMC4p\r\nLSwzOko+MzZGNywtQFdBRkxOUlNSMj5aYVpQYEpRUk//2wBDAQ4ODhMREyYVFSZPNS01T09PT09P\r\nT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0//wAARCAAkACoDASEA\r\nAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA\r\nAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3\r\nODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm\r\np6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA\r\nAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx\r\nBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK\r\nU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3\r\nuLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDpWXAw\r\nenaq8gVepAq0SV32/wB4fnUDAdiDTGRlaNh9T+dSBvSDPTrjpTwba1toBdWJlnlUsVP8I/GmIYs+\r\nl4+bSnX6N/8AXqIrpV3J5NqlxBclSUBRsOR7nigZmKxkQMylTyCD2I60nFAjdhsIbq7Erq3mICUI\r\nJwD2JFTNbaurY/twrjpmyX5v1zUsaAwa4Rxq0eR62R5+tQuPEeQFvdJLg8eZE4/rQgMKK0uWv724\r\nu54PtJfE8MWdqnsR9RT/ACz6H8qoRd1bVptEEE8SQu8wYIkjY3Yx0OaxZviNerFgQWfmE4OA7bPc\r\n/wD66mwy4fHVoyIV1bD7RvH2QlQfbvUcXj2TczPp8NwycCVJNm4euG6UWGTaZcNq1tdauYxCLiXa\r\nEDZwB61YwaZJh/EuRo10srj5EcgdutS6c1hd2kHn6LphJRckQkE8A+tMZtxeF9AlSF20mAGTOQrO\r\nB/Os7xp4Z0bSfDVxc2NkscygBX3MSvPbJpAL4MHm+Cogxx++bOO/Irof7Ph/vP8AmP8ACmI//9k=",
                        CarBrand = "品牌" + random.Next(1, 10),
                        CarType = random.Next(1, 21),
                        CarColor = random.Next(1, 21),
                        PlateNumber = "京A0000" + random.Next(1, 9),
                        PlateType = random.Next(1, 29)
                    };
                    WebSocketMiddleware.Broadcast(url, vehicle);

                    VideoStructAdapterData pedestrain = new VideoStructAdapterData
                    {
                        VideoStructType = VideoStructType.行人,
                        ChannelId = $"channel_{channelId}_{i}",
                        LaneId = $"{j:D2}",
                        Timestamp = TimeStampConvert.ToUtcTimeStamp(now),
                        Feature = "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",
                        Image = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAA0JCgsKCA0LCgsODg0PEyAVExISEyccHhcgLikxMC4p\r\nLSwzOko+MzZGNywtQFdBRkxOUlNSMj5aYVpQYEpRUk//2wBDAQ4ODhMREyYVFSZPNS01T09PT09P\r\nT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0//wAARCAAkACoDASEA\r\nAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA\r\nAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3\r\nODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm\r\np6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA\r\nAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx\r\nBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK\r\nU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3\r\nuLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDpWXAw\r\nenaq8gVepAq0SV32/wB4fnUDAdiDTGRlaNh9T+dSBvSDPTrjpTwba1toBdWJlnlUsVP8I/GmIYs+\r\nl4+bSnX6N/8AXqIrpV3J5NqlxBclSUBRsOR7nigZmKxkQMylTyCD2I60nFAjdhsIbq7Erq3mICUI\r\nJwD2JFTNbaurY/twrjpmyX5v1zUsaAwa4Rxq0eR62R5+tQuPEeQFvdJLg8eZE4/rQgMKK0uWv724\r\nu54PtJfE8MWdqnsR9RT/ACz6H8qoRd1bVptEEE8SQu8wYIkjY3Yx0OaxZviNerFgQWfmE4OA7bPc\r\n/wD66mwy4fHVoyIV1bD7RvH2QlQfbvUcXj2TczPp8NwycCVJNm4euG6UWGTaZcNq1tdauYxCLiXa\r\nEDZwB61YwaZJh/EuRo10srj5EcgdutS6c1hd2kHn6LphJRckQkE8A+tMZtxeF9AlSF20mAGTOQrO\r\nB/Os7xp4Z0bSfDVxc2NkscygBX3MSvPbJpAL4MHm+Cogxx++bOO/Irof7Ph/vP8AmP8ACmI//9k=",
                        Sex = random.Next(1, 2),
                        Age = random.Next(1, 5),
                        UpperColor = random.Next(1, 9)
                    };
                    WebSocketMiddleware.Broadcast(url, pedestrain);
                }

                channelId += 1;
            }
        }
    }
}