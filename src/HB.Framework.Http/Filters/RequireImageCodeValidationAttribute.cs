﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    public sealed class RequireImageCodeValidationAttribute : ActionFilterAttribute
    {
        public string ImageCodeParameterName { get; set; }

        public string ImageCodeSessionName { get; set; }

        public RequireImageCodeValidationAttribute(string parameterName) : this(parameterName, parameterName) { }
        
        public RequireImageCodeValidationAttribute(string parameterName, string sessionName)
        {
            ImageCodeParameterName = parameterName;
            ImageCodeSessionName = sessionName;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (HandleValidation(context.HttpContext))
            {
                base.OnActionExecuting(context);
            }
            else
            {
                HandleFailRequest(context);
            }
        }

        private static void HandleFailRequest(ActionExecutingContext context)
        {
            if (context.HttpContext.Request.Path.StartsWithSegments("/api", GlobalSettings.Comparison))
            {
                context.Result = new BadRequestObjectResult("ImageCode Error.");
            }
            else
            {
                context.Result = new RedirectResult("~/Error");
            }
        }

        private bool HandleValidation(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            string imageCode = httpContext.GetValueFromRequest(ImageCodeParameterName);

            if (string.IsNullOrWhiteSpace(imageCode))
            {
                return false;
            }

            string cachedCode = httpContext.Session.GetString(ImageCodeParameterName);

            return imageCode.Equals(cachedCode, GlobalSettings.Comparison);
        }
    }
}
