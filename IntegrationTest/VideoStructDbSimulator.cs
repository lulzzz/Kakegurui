using System;
using System.Collections.Generic;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.Extensions.DependencyInjection;
using MomobamiKirari.Data;
using MomobamiKirari.DataFlow;
using MomobamiKirari.Models;
using ItsukiSumeragi.Codes.Flow;

namespace IntegrationTest
{
    /// <summary>
    /// smo视频结构化模拟器
    /// </summary>
    public class VideoStructDbSimulator
    {
        public static void CreateData(IServiceProvider serviceProvider, List<TrafficDevice> devices, DataCreateMode mode, DateTime startDate, DateTime endDate, bool initDatabase = false)
        {
            if (initDatabase)
            {
                using (FlowContext context = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<FlowContext>())
                {
                    context.Database.EnsureDeleted();
                    context.Database.EnsureCreated();
                }
            }

            DateTime minTime = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, startDate);
            DateTime maxTime = TimePointConvert.NextTimePoint(BranchDbConvert.DateLevel, minTime);
            VideoBranchBlock branch = new VideoBranchBlock(serviceProvider);
            branch.Open(minTime, maxTime);
            Random random = new Random();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                int value = 1;
                for (int i = 0; i < 1440; ++i)
                {
                    DateTime startTime = startDate.AddMinutes(i);
                    foreach (TrafficDevice device in devices)
                    {
                        foreach (var relation in device.Device_Channels)
                        {
                            foreach (TrafficLane lane in relation.Channel.Lanes)
                            {
                                VideoVehicle videoVehicle;
                                VideoBike bike;
                                VideoPedestrain pedestrain;
                                if (mode == DataCreateMode.Fixed)
                                {
                                    videoVehicle = new VideoVehicle
                                    {
                                        DataId = lane.DataId,
                                        DateTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, 0),
                                        Image = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAA0JCgsKCA0LCgsODg0PEyAVExISEyccHhcgLikxMC4p\r\nLSwzOko+MzZGNywtQFdBRkxOUlNSMj5aYVpQYEpRUk//2wBDAQ4ODhMREyYVFSZPNS01T09PT09P\r\nT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0//wAARCAAkACoDASEA\r\nAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA\r\nAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3\r\nODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm\r\np6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA\r\nAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx\r\nBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK\r\nU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3\r\nuLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDpWXAw\r\nenaq8gVepAq0SV32/wB4fnUDAdiDTGRlaNh9T+dSBvSDPTrjpTwba1toBdWJlnlUsVP8I/GmIYs+\r\nl4+bSnX6N/8AXqIrpV3J5NqlxBclSUBRsOR7nigZmKxkQMylTyCD2I60nFAjdhsIbq7Erq3mICUI\r\nJwD2JFTNbaurY/twrjpmyX5v1zUsaAwa4Rxq0eR62R5+tQuPEeQFvdJLg8eZE4/rQgMKK0uWv724\r\nu54PtJfE8MWdqnsR9RT/ACz6H8qoRd1bVptEEE8SQu8wYIkjY3Yx0OaxZviNerFgQWfmE4OA7bPc\r\n/wD66mwy4fHVoyIV1bD7RvH2QlQfbvUcXj2TczPp8NwycCVJNm4euG6UWGTaZcNq1tdauYxCLiXa\r\nEDZwB61YwaZJh/EuRo10srj5EcgdutS6c1hd2kHn6LphJRckQkE8A+tMZtxeF9AlSF20mAGTOQrO\r\nB/Os7xp4Z0bSfDVxc2NkscygBX3MSvPbJpAL4MHm+Cogxx++bOO/Irof7Ph/vP8AmP8ACmI//9k=",
                                        Feature = "",
                                        CarType = 1,
                                        CarBrand = "brand" + 1,
                                        CarColor = 1,
                                        PlateNumber = "京A0000" + 1,
                                        PlateType = 1
                                    };
                                    bike = new VideoBike
                                    {
                                        DataId = lane.DataId,
                                        DateTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, 0),
                                        Image = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAA0JCgsKCA0LCgsODg0PEyAVExISEyccHhcgLikxMC4p\r\nLSwzOko+MzZGNywtQFdBRkxOUlNSMj5aYVpQYEpRUk//2wBDAQ4ODhMREyYVFSZPNS01T09PT09P\r\nT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0//wAARCAAkACoDASEA\r\nAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA\r\nAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3\r\nODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm\r\np6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA\r\nAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx\r\nBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK\r\nU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3\r\nuLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDpWXAw\r\nenaq8gVepAq0SV32/wB4fnUDAdiDTGRlaNh9T+dSBvSDPTrjpTwba1toBdWJlnlUsVP8I/GmIYs+\r\nl4+bSnX6N/8AXqIrpV3J5NqlxBclSUBRsOR7nigZmKxkQMylTyCD2I60nFAjdhsIbq7Erq3mICUI\r\nJwD2JFTNbaurY/twrjpmyX5v1zUsaAwa4Rxq0eR62R5+tQuPEeQFvdJLg8eZE4/rQgMKK0uWv724\r\nu54PtJfE8MWdqnsR9RT/ACz6H8qoRd1bVptEEE8SQu8wYIkjY3Yx0OaxZviNerFgQWfmE4OA7bPc\r\n/wD66mwy4fHVoyIV1bD7RvH2QlQfbvUcXj2TczPp8NwycCVJNm4euG6UWGTaZcNq1tdauYxCLiXa\r\nEDZwB61YwaZJh/EuRo10srj5EcgdutS6c1hd2kHn6LphJRckQkE8A+tMZtxeF9AlSF20mAGTOQrO\r\nB/Os7xp4Z0bSfDVxc2NkscygBX3MSvPbJpAL4MHm+Cogxx++bOO/Irof7Ph/vP8AmP8ACmI//9k=",
                                        Feature = "",
                                        BikeType = (int)NonVehicle.自行车
                                    };
                                    pedestrain = new VideoPedestrain
                                    {
                                        DataId = lane.DataId,
                                        DateTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, 0),
                                        Image = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAA0JCgsKCA0LCgsODg0PEyAVExISEyccHhcgLikxMC4p\r\nLSwzOko+MzZGNywtQFdBRkxOUlNSMj5aYVpQYEpRUk//2wBDAQ4ODhMREyYVFSZPNS01T09PT09P\r\nT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0//wAARCAAkACoDASEA\r\nAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA\r\nAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3\r\nODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm\r\np6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA\r\nAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx\r\nBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK\r\nU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3\r\nuLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDpWXAw\r\nenaq8gVepAq0SV32/wB4fnUDAdiDTGRlaNh9T+dSBvSDPTrjpTwba1toBdWJlnlUsVP8I/GmIYs+\r\nl4+bSnX6N/8AXqIrpV3J5NqlxBclSUBRsOR7nigZmKxkQMylTyCD2I60nFAjdhsIbq7Erq3mICUI\r\nJwD2JFTNbaurY/twrjpmyX5v1zUsaAwa4Rxq0eR62R5+tQuPEeQFvdJLg8eZE4/rQgMKK0uWv724\r\nu54PtJfE8MWdqnsR9RT/ACz6H8qoRd1bVptEEE8SQu8wYIkjY3Yx0OaxZviNerFgQWfmE4OA7bPc\r\n/wD66mwy4fHVoyIV1bD7RvH2QlQfbvUcXj2TczPp8NwycCVJNm4euG6UWGTaZcNq1tdauYxCLiXa\r\nEDZwB61YwaZJh/EuRo10srj5EcgdutS6c1hd2kHn6LphJRckQkE8A+tMZtxeF9AlSF20mAGTOQrO\r\nB/Os7xp4Z0bSfDVxc2NkscygBX3MSvPbJpAL4MHm+Cogxx++bOO/Irof7Ph/vP8AmP8ACmI//9k=",
                                        Feature = "",
                                        Age = (int)Age.儿童,
                                        Sex = (int)Sex.女,
                                        UpperColor = (int)UpperColor.红
                                    };
                                }
                                else if (mode == DataCreateMode.Sequence)
                                {
                                    int temp = value;
                                    value += 1;
                                    videoVehicle = new VideoVehicle
                                    {
                                        DataId = lane.DataId,
                                        DateTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, 0),
                                        Image = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAA0JCgsKCA0LCgsODg0PEyAVExISEyccHhcgLikxMC4p\r\nLSwzOko+MzZGNywtQFdBRkxOUlNSMj5aYVpQYEpRUk//2wBDAQ4ODhMREyYVFSZPNS01T09PT09P\r\nT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0//wAARCAAkACoDASEA\r\nAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA\r\nAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3\r\nODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm\r\np6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA\r\nAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx\r\nBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK\r\nU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3\r\nuLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDpWXAw\r\nenaq8gVepAq0SV32/wB4fnUDAdiDTGRlaNh9T+dSBvSDPTrjpTwba1toBdWJlnlUsVP8I/GmIYs+\r\nl4+bSnX6N/8AXqIrpV3J5NqlxBclSUBRsOR7nigZmKxkQMylTyCD2I60nFAjdhsIbq7Erq3mICUI\r\nJwD2JFTNbaurY/twrjpmyX5v1zUsaAwa4Rxq0eR62R5+tQuPEeQFvdJLg8eZE4/rQgMKK0uWv724\r\nu54PtJfE8MWdqnsR9RT/ACz6H8qoRd1bVptEEE8SQu8wYIkjY3Yx0OaxZviNerFgQWfmE4OA7bPc\r\n/wD66mwy4fHVoyIV1bD7RvH2QlQfbvUcXj2TczPp8NwycCVJNm4euG6UWGTaZcNq1tdauYxCLiXa\r\nEDZwB61YwaZJh/EuRo10srj5EcgdutS6c1hd2kHn6LphJRckQkE8A+tMZtxeF9AlSF20mAGTOQrO\r\nB/Os7xp4Z0bSfDVxc2NkscygBX3MSvPbJpAL4MHm+Cogxx++bOO/Irof7Ph/vP8AmP8ACmI//9k=",
                                        Feature = "",
                                        CarType = 1,
                                        CarBrand = "brand" + temp,
                                        CarColor = 1,
                                        PlateNumber = "京A0000" + 1,
                                        PlateType = 1
                                    };
                                    bike = new VideoBike
                                    {
                                        DataId = lane.DataId,
                                        DateTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, 0),
                                        Image = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAA0JCgsKCA0LCgsODg0PEyAVExISEyccHhcgLikxMC4p\r\nLSwzOko+MzZGNywtQFdBRkxOUlNSMj5aYVpQYEpRUk//2wBDAQ4ODhMREyYVFSZPNS01T09PT09P\r\nT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0//wAARCAAkACoDASEA\r\nAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA\r\nAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3\r\nODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm\r\np6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA\r\nAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx\r\nBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK\r\nU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3\r\nuLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDpWXAw\r\nenaq8gVepAq0SV32/wB4fnUDAdiDTGRlaNh9T+dSBvSDPTrjpTwba1toBdWJlnlUsVP8I/GmIYs+\r\nl4+bSnX6N/8AXqIrpV3J5NqlxBclSUBRsOR7nigZmKxkQMylTyCD2I60nFAjdhsIbq7Erq3mICUI\r\nJwD2JFTNbaurY/twrjpmyX5v1zUsaAwa4Rxq0eR62R5+tQuPEeQFvdJLg8eZE4/rQgMKK0uWv724\r\nu54PtJfE8MWdqnsR9RT/ACz6H8qoRd1bVptEEE8SQu8wYIkjY3Yx0OaxZviNerFgQWfmE4OA7bPc\r\n/wD66mwy4fHVoyIV1bD7RvH2QlQfbvUcXj2TczPp8NwycCVJNm4euG6UWGTaZcNq1tdauYxCLiXa\r\nEDZwB61YwaZJh/EuRo10srj5EcgdutS6c1hd2kHn6LphJRckQkE8A+tMZtxeF9AlSF20mAGTOQrO\r\nB/Os7xp4Z0bSfDVxc2NkscygBX3MSvPbJpAL4MHm+Cogxx++bOO/Irof7Ph/vP8AmP8ACmI//9k=",
                                        Feature = "",
                                        BikeType = (temp % 2 == 0 ? 2 : 3)
                                    };
                                    pedestrain = new VideoPedestrain
                                    {
                                        DataId = lane.DataId,
                                        DateTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, 0),
                                        Image = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAA0JCgsKCA0LCgsODg0PEyAVExISEyccHhcgLikxMC4p\r\nLSwzOko+MzZGNywtQFdBRkxOUlNSMj5aYVpQYEpRUk//2wBDAQ4ODhMREyYVFSZPNS01T09PT09P\r\nT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0//wAARCAAkACoDASEA\r\nAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA\r\nAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3\r\nODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm\r\np6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA\r\nAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx\r\nBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK\r\nU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3\r\nuLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDpWXAw\r\nenaq8gVepAq0SV32/wB4fnUDAdiDTGRlaNh9T+dSBvSDPTrjpTwba1toBdWJlnlUsVP8I/GmIYs+\r\nl4+bSnX6N/8AXqIrpV3J5NqlxBclSUBRsOR7nigZmKxkQMylTyCD2I60nFAjdhsIbq7Erq3mICUI\r\nJwD2JFTNbaurY/twrjpmyX5v1zUsaAwa4Rxq0eR62R5+tQuPEeQFvdJLg8eZE4/rQgMKK0uWv724\r\nu54PtJfE8MWdqnsR9RT/ACz6H8qoRd1bVptEEE8SQu8wYIkjY3Yx0OaxZviNerFgQWfmE4OA7bPc\r\n/wD66mwy4fHVoyIV1bD7RvH2QlQfbvUcXj2TczPp8NwycCVJNm4euG6UWGTaZcNq1tdauYxCLiXa\r\nEDZwB61YwaZJh/EuRo10srj5EcgdutS6c1hd2kHn6LphJRckQkE8A+tMZtxeF9AlSF20mAGTOQrO\r\nB/Os7xp4Z0bSfDVxc2NkscygBX3MSvPbJpAL4MHm+Cogxx++bOO/Irof7Ph/vP8AmP8ACmI//9k=",
                                        Feature = "",
                                        Age = (int)Age.儿童,
                                        Sex = (temp % 2 == 0 ? 1 : 2),
                                        UpperColor = (int)UpperColor.红
                                    };
                                }
                                else
                                {
                                    videoVehicle = new VideoVehicle
                                    {
                                        DataId = lane.DataId,
                                        DateTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, 0),
                                        Image = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAA0JCgsKCA0LCgsODg0PEyAVExISEyccHhcgLikxMC4p\r\nLSwzOko+MzZGNywtQFdBRkxOUlNSMj5aYVpQYEpRUk//2wBDAQ4ODhMREyYVFSZPNS01T09PT09P\r\nT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0//wAARCAAkACoDASEA\r\nAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA\r\nAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3\r\nODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm\r\np6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA\r\nAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx\r\nBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK\r\nU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3\r\nuLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDpWXAw\r\nenaq8gVepAq0SV32/wB4fnUDAdiDTGRlaNh9T+dSBvSDPTrjpTwba1toBdWJlnlUsVP8I/GmIYs+\r\nl4+bSnX6N/8AXqIrpV3J5NqlxBclSUBRsOR7nigZmKxkQMylTyCD2I60nFAjdhsIbq7Erq3mICUI\r\nJwD2JFTNbaurY/twrjpmyX5v1zUsaAwa4Rxq0eR62R5+tQuPEeQFvdJLg8eZE4/rQgMKK0uWv724\r\nu54PtJfE8MWdqnsR9RT/ACz6H8qoRd1bVptEEE8SQu8wYIkjY3Yx0OaxZviNerFgQWfmE4OA7bPc\r\n/wD66mwy4fHVoyIV1bD7RvH2QlQfbvUcXj2TczPp8NwycCVJNm4euG6UWGTaZcNq1tdauYxCLiXa\r\nEDZwB61YwaZJh/EuRo10srj5EcgdutS6c1hd2kHn6LphJRckQkE8A+tMZtxeF9AlSF20mAGTOQrO\r\nB/Os7xp4Z0bSfDVxc2NkscygBX3MSvPbJpAL4MHm+Cogxx++bOO/Irof7Ph/vP8AmP8ACmI//9k=",
                                        Feature = "",
                                        CarType = random.Next(1, 21),
                                        CarBrand = "brand" + random.Next(1, 100),
                                        CarColor = random.Next(1, 13),
                                        PlateNumber = "京A0000" + random.Next(1, 9),
                                        PlateType = random.Next(1, 29)
                                    };
                                    bike = new VideoBike
                                    {
                                        DataId = lane.DataId,
                                        DateTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, 0),
                                        Image = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAA0JCgsKCA0LCgsODg0PEyAVExISEyccHhcgLikxMC4p\r\nLSwzOko+MzZGNywtQFdBRkxOUlNSMj5aYVpQYEpRUk//2wBDAQ4ODhMREyYVFSZPNS01T09PT09P\r\nT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0//wAARCAAkACoDASEA\r\nAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA\r\nAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3\r\nODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm\r\np6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA\r\nAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx\r\nBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK\r\nU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3\r\nuLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDpWXAw\r\nenaq8gVepAq0SV32/wB4fnUDAdiDTGRlaNh9T+dSBvSDPTrjpTwba1toBdWJlnlUsVP8I/GmIYs+\r\nl4+bSnX6N/8AXqIrpV3J5NqlxBclSUBRsOR7nigZmKxkQMylTyCD2I60nFAjdhsIbq7Erq3mICUI\r\nJwD2JFTNbaurY/twrjpmyX5v1zUsaAwa4Rxq0eR62R5+tQuPEeQFvdJLg8eZE4/rQgMKK0uWv724\r\nu54PtJfE8MWdqnsR9RT/ACz6H8qoRd1bVptEEE8SQu8wYIkjY3Yx0OaxZviNerFgQWfmE4OA7bPc\r\n/wD66mwy4fHVoyIV1bD7RvH2QlQfbvUcXj2TczPp8NwycCVJNm4euG6UWGTaZcNq1tdauYxCLiXa\r\nEDZwB61YwaZJh/EuRo10srj5EcgdutS6c1hd2kHn6LphJRckQkE8A+tMZtxeF9AlSF20mAGTOQrO\r\nB/Os7xp4Z0bSfDVxc2NkscygBX3MSvPbJpAL4MHm+Cogxx++bOO/Irof7Ph/vP8AmP8ACmI//9k=",
                                        Feature = "",
                                        BikeType = random.Next(2, 3)
                                    };
                                    pedestrain = new VideoPedestrain
                                    {
                                        DataId = lane.DataId,
                                        DateTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, 0),
                                        Image = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAA0JCgsKCA0LCgsODg0PEyAVExISEyccHhcgLikxMC4p\r\nLSwzOko+MzZGNywtQFdBRkxOUlNSMj5aYVpQYEpRUk//2wBDAQ4ODhMREyYVFSZPNS01T09PT09P\r\nT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0//wAARCAAkACoDASEA\r\nAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA\r\nAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3\r\nODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm\r\np6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA\r\nAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx\r\nBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK\r\nU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3\r\nuLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDpWXAw\r\nenaq8gVepAq0SV32/wB4fnUDAdiDTGRlaNh9T+dSBvSDPTrjpTwba1toBdWJlnlUsVP8I/GmIYs+\r\nl4+bSnX6N/8AXqIrpV3J5NqlxBclSUBRsOR7nigZmKxkQMylTyCD2I60nFAjdhsIbq7Erq3mICUI\r\nJwD2JFTNbaurY/twrjpmyX5v1zUsaAwa4Rxq0eR62R5+tQuPEeQFvdJLg8eZE4/rQgMKK0uWv724\r\nu54PtJfE8MWdqnsR9RT/ACz6H8qoRd1bVptEEE8SQu8wYIkjY3Yx0OaxZviNerFgQWfmE4OA7bPc\r\n/wD66mwy4fHVoyIV1bD7RvH2QlQfbvUcXj2TczPp8NwycCVJNm4euG6UWGTaZcNq1tdauYxCLiXa\r\nEDZwB61YwaZJh/EuRo10srj5EcgdutS6c1hd2kHn6LphJRckQkE8A+tMZtxeF9AlSF20mAGTOQrO\r\nB/Os7xp4Z0bSfDVxc2NkscygBX3MSvPbJpAL4MHm+Cogxx++bOO/Irof7Ph/vP8AmP8ACmI//9k=",
                                        Feature = "",
                                        Age = (int)Age.儿童,
                                        Sex = (int)Sex.女,
                                        UpperColor = random.Next(1, 9)
                                    };
                                }
                                branch.Post(videoVehicle);
                                branch.Post(bike);
                                branch.Post(pedestrain);
                            }
                        }
                    }
                }
            }

            DateTime currentTime = TimePointConvert.CurrentTimePoint(BranchDbConvert.DateLevel, DateTime.Now);
            branch.Close();
            if (currentTime != minTime)
            {
                using (FlowContext context = serviceProvider.GetRequiredService<FlowContext>())
                {
                    context.ChangeBikeTable(BranchDbConvert.GetTableName(minTime));
                    context.ChangePedestrainTable(BranchDbConvert.GetTableName(minTime));
                    context.ChangeVehicleTable(BranchDbConvert.GetTableName(minTime));
                }
            }
        }

    }
}
