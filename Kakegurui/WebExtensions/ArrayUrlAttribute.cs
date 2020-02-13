using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// 字符串数组url参数转换
    /// </summary>
    public class StringsUrlAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// 参数名
        /// </summary>
        private readonly string _parameterName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parameterName">参数名</param>
        public StringsUrlAttribute(string parameterName)
        {
            _parameterName = parameterName;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            if (actionContext.ActionArguments.ContainsKey(_parameterName))
            {
                if (actionContext.RouteData.Values.ContainsKey(_parameterName))
                {
                    actionContext.ActionArguments[_parameterName] =
                        actionContext.RouteData.Values[_parameterName]
                            .ToString()
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .ToArray();
                }
                else if (actionContext.HttpContext.Request.Query.ContainsKey(_parameterName))
                {
                    actionContext.ActionArguments[_parameterName] =
                        actionContext.HttpContext.Request.Query[_parameterName]
                            .ToString()
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .ToArray();
                }
            }
        }
    }

    /// <summary>
    /// 数字数组url参数转换
    /// </summary>
    public class IntegersUrlAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// 参数名
        /// </summary>
        private readonly string _parameterName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parameterName">参数名</param>
        public IntegersUrlAttribute(string parameterName)
        {
            _parameterName = parameterName;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            if (actionContext.ActionArguments.ContainsKey(_parameterName))
            {
                if (actionContext.RouteData.Values.ContainsKey(_parameterName))
                {
                    actionContext.ActionArguments[_parameterName] =
                        actionContext.RouteData.Values[_parameterName]
                            .ToString()
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(int.Parse)
                            .ToArray();
                }
                else if (actionContext.HttpContext.Request.Query.ContainsKey(_parameterName))
                {
                    actionContext.ActionArguments[_parameterName] =
                        actionContext.HttpContext.Request.Query[_parameterName]
                            .ToString()
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(int.Parse)
                            .ToArray();
                }
            }
        }
    }

    /// <summary>
    /// 时间数组url参数转换
    /// </summary>
    public class DateTimeUrlAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// 参数名
        /// </summary>
        private readonly string _parameterName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parameterName">参数名</param>
        public DateTimeUrlAttribute(string parameterName)
        {
            _parameterName = parameterName;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            if (actionContext.ActionArguments.ContainsKey(_parameterName))
            {
                if (actionContext.RouteData.Values.ContainsKey(_parameterName))
                {
                    actionContext.ActionArguments[_parameterName] =
                        actionContext.RouteData.Values[_parameterName]
                            .ToString()
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(DateTime.Parse)
                            .ToArray();
                }
                else if (actionContext.HttpContext.Request.Query.ContainsKey(_parameterName))
                {
                    actionContext.ActionArguments[_parameterName] =
                        actionContext.HttpContext.Request.Query[_parameterName]
                            .ToString()
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(DateTime.Parse)
                            .ToArray();
                }
            }
        }
    }
}
