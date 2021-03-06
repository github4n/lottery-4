using System.Net;
using Colin.Lottery.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Colin.Lottery.WebApp.Filters
{
    /// <summary>
    /// 全局异常过滤器
    /// </summary>
    public class GlobalExceptionFilter:IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            //提交异常信息
            ExceptionlessUtil.TraceException(context.Exception);

            //转到错误页面
            context.Result = new RedirectResult("/error");
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            
            //标记已处理
            context.ExceptionHandled = true;
        }
    }
}