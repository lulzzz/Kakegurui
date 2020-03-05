using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomobamiKirari.Managers;
using MomobamiKirari.Models;

namespace MomobamiKirari.Controllers
{
    /// <summary>
    /// 设备
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        /// <summary>
        /// 设备数据库操作实例
        /// </summary>
        private readonly DevicesManager _manager;

        /// <summary>
        /// 数据库实例
        /// </summary>
        /// <param name="manager">设备数据库操作实例</param>
        public DevicesController(DevicesManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// 查询流量设备集合
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="deviceModel">设备型号</param>
        /// <param name="deviceStatus">设备状态</param>
        /// <param name="ip">设备ip</param>
        /// <param name="nodeUrl">所属节点</param>
        /// <param name="order">排序方式</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<FlowDevice> GetList([FromQuery] string deviceName, [FromQuery] int deviceModel, [FromQuery] int deviceStatus, [FromQuery] string ip, [FromQuery] string nodeUrl, [FromQuery]string order, [FromQuery] int pageNum,[FromQuery] int pageSize)
        {
            return _manager.GetList(deviceName, deviceModel, deviceStatus, ip, nodeUrl, order,
                pageNum, pageSize);
        }

        /// <summary>
        /// 查询流量设备
        /// </summary>
        /// <param name="deviceId">设备编号</param>
        /// <returns>查询结果</returns>
        [HttpGet("{deviceId}")]
        public IActionResult Get([FromRoute] int deviceId)
        {
            return _manager.Get(deviceId);
        }

        /// <summary>
        /// 添加设备
        /// </summary>
        /// <param name="deviceInsert">设备信息</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public IActionResult Add([FromBody] FlowDeviceInsert deviceInsert)
        {
            return _manager.Add(deviceInsert, User?.Identity?.Name);
        }
        
        /// <summary>
        /// 导入设备
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns>导入结果</returns>
        [HttpPost("import")]
        public IActionResult Import(IFormFile file)
        {
            return _manager.Import(file, User?.Identity?.Name);
        }

        /// <summary>
        /// 更新设备
        /// </summary>
        /// <param name="deviceUpdate">设备信息</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public IActionResult Update([FromBody] FlowDeviceUpdate deviceUpdate)
        {
            return _manager.Update(deviceUpdate, User?.Identity?.Name);
        }

        /// <summary>
        /// 更新设备标注状态
        /// </summary>
        /// <param name="deviceUpdateLocation">设备标注状态</param>
        /// <returns>更新结果</returns>
        [HttpPut("location")]
        public IActionResult UpdateLocation([FromBody] FlowDeviceUpdateLocation deviceUpdateLocation)
        {
            return _manager.UpdateLocation(deviceUpdateLocation);
        }

        /// <summary>
        /// 更新设备状态
        /// </summary>
        /// <param name="deviceUpdateStatus">设备状态</param>
        /// <returns>更新结果</returns>
        [HttpPut("status")]
        public IActionResult UpdateStatus([FromBody] FlowDeviceUpdateStatus deviceUpdateStatus)
        {
            return _manager.UpdateStatus(deviceUpdateStatus);
        }

        /// <summary>
        /// 删除流量设备
        /// </summary>
        /// <param name="deviceId">设备编号</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{deviceId}")]
        public IActionResult Remove([FromRoute] int deviceId)
        {
            return _manager.Remove(deviceId, User?.Identity?.Name);
        }
    }
}