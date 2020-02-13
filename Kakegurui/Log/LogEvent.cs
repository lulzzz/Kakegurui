﻿
namespace Kakegurui.Log
{
    /// <summary>
    /// 日志事件
    /// </summary>
    public enum LogEvent
    {
        系统=70000,
        套接字=70002,
        字节流=70003,
        定时任务=70004,
        配置项 = 70005,
        设备检查=70006,
        分支切换=70007,
        数据适配=70008,

        编辑设备=71000,
        编辑通道=71001,
        编辑路口=71002,
        编辑路段=71003,
        编辑地点=71004,

        流量数据块=72001,
        视频数据块=72002,
        路段流量=72003,
        路段状态=72004,
        流量查询=72005,

        高点数据块 = 73001,
        事件数据块 = 73002,

        违法数据块 = 74001,
        流媒体检查 = 74002,

        编辑违法点位=75001,
        编辑违法设备=75002,
        编辑违法类型=75003,
        编辑图片命名规则=75004,

        数据监控 =79000
    }
}