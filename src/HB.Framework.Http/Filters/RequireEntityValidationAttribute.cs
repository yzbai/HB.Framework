﻿
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    public class RequireEntityValidationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                context.Result = new BadRequestObjectResult(context.ModelState);
            }
        }
    }
}
