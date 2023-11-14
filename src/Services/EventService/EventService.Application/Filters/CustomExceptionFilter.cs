using EventService.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace EventService.Application.Filters
{
    public class CustomExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            switch (context.Exception)
            {
                case InternalErrorException customException:
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Result = new JsonResult(new { customException.Code, customException.Message });
                    context.ExceptionHandled = true;
                    break;
                case NotFoundException:
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Result = new NotFoundResult();
                    context.ExceptionHandled = true;
                    break;
                case ForbiddenAccessException:
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    context.Result = new ForbidResult();
                    context.ExceptionHandled = true;
                    break;
                default:
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Result = new JsonResult(new
                    {
                        Code = (int)HttpStatusCode.InternalServerError,
                        Message = "Something went wrong..."
                    });
                    context.ExceptionHandled = true;
                    break;
            }
        }
    }
}
