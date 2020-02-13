using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ItsukiSumeragi.DataFlow;
using Kakegurui.Log;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NishinotouinYuriko.Models;

namespace NishinotouinYuriko.DataFlow
{
    /// <summary>
    /// 违法图片数据块
    /// </summary>
    public class ViolationImageBlock:TrafficActionBlock<ViolationStruct>
    {
        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// 文件保存路径
        /// </summary>
        private readonly string _filePath;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">实例工厂</param>
        public ViolationImageBlock(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<ViolationImageBlock>>();
            _filePath = serviceProvider.GetRequiredService<IConfiguration>().GetValue("FilePath", AppDomain.CurrentDomain.BaseDirectory);
        }

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="content">文件内容</param>
        private void SaveImage(string filePath, string content)
        {
            try
            {
                byte[] buffer = Convert.FromBase64String(content);
                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    using (Bitmap bitmap = new Bitmap(ms))
                    {
                        bitmap.Save(filePath, ImageFormat.Jpeg);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError((int)LogEvent.违法数据块, ex, "保存违法图片错误");
            }
        }

        /// <summary>
        /// 保存视频
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="content">文件内容</param>
        private void SaveViode(string filePath, string content)
        {
            try
            {
                FileStream fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
                fs.Write(Convert.FromBase64String(content));
                fs.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError((int)LogEvent.违法数据块, ex, "保存违法视频错误");
            }
        }

        protected override void Handle(ViolationStruct t)
        {
            string directory = Path.Combine(_filePath, t.DateTime.ToString("yyyy-MM"));
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!string.IsNullOrEmpty(t.ImageLink1))
            {
                SaveImage(Path.Combine(directory, Path.GetFileName(t.ImageLink1)), t.Image1);
            }
            if (!string.IsNullOrEmpty(t.ImageLink2))
            {
                SaveImage(Path.Combine(directory, Path.GetFileName(t.ImageLink2)), t.Image2);
            }
            if (!string.IsNullOrEmpty(t.ImageLink3))
            {
                SaveImage(Path.Combine(directory, Path.GetFileName(t.ImageLink3)), t.Image3);
            }
            if (!string.IsNullOrEmpty(t.ImageLink4))
            {
                SaveImage(Path.Combine(directory, Path.GetFileName(t.ImageLink4)), t.Image4);
            }
            if (!string.IsNullOrEmpty(t.ImageLink5))
            {
                SaveImage(Path.Combine(directory, Path.GetFileName(t.ImageLink5)), t.Image5);
            }
            if (!string.IsNullOrEmpty(t.VideoLink))
            {
                SaveViode(Path.Combine(directory, Path.GetFileName(t.VideoLink)), t.Video);
            }
        }
    }
}
