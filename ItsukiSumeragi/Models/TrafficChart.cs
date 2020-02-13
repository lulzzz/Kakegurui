
using System.Collections.Generic;

namespace ItsukiSumeragi.Models
{
    /// <summary>
    /// 流量图表
    /// </summary>
    /// <typeparam name="T">横轴类型</typeparam>
    /// <typeparam name="U">纵轴类型</typeparam>
    public class TrafficChart<T, U>
    {
        /// <summary>
        /// x轴
        /// </summary>
        public T Axis { get; set; }

        /// <summary>
        /// y轴
        /// </summary>
        public U Value { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// 流量图表
    /// </summary>
    /// <typeparam name="T">横轴类型</typeparam>
    /// <typeparam name="U">纵轴类型</typeparam>
    /// <typeparam name="V">数据类型</typeparam>
    public class TrafficChart<T, U, V>
    {
        /// <summary>
        /// x轴
        /// </summary>
        public T Axis { get; set; }

        /// <summary>
        /// y轴
        /// </summary>
        public U Value { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// Z轴或自定义数据
        /// </summary>
        public V Data { get; set; }
    }

    /// <summary>
    /// 分组图表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class TrafficGroupChart<T, U, V>:TrafficChart<T,U,V>
    {
        public List<TrafficGroupChart<T, U, V>> Datas { get; set; }
    }

}
