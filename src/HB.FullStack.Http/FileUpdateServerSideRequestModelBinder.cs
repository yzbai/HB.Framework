﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace HB.FullStack.Common.Api
{
    public class FileUpdateServerSideRequestModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            ValueProviderResult valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            Type modelType = bindingContext.ModelType;

            try
            {
                object? model = SerializeUtil.FromJson(modelType, valueProviderResult.FirstValue);

                if (model == null)
                {
                    bindingContext.ModelState.AddModelError("Request", "Request lack some data.");
                    bindingContext.Result = ModelBindingResult.Failed();

                    return Task.CompletedTask;
                }

                modelType.GetProperty("Files")!.SetValue(model, bindingContext.HttpContext.Request.Form.Files.GetFiles("Files").ToList());


                bindingContext.Result = ModelBindingResult.Success(model);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                GlobalSettings.Logger?.LogWarning(ex, $"FileUpdateServerSideRequestModelBinder出错.{valueProviderResult.FirstValue}");

                bindingContext.ModelState.AddModelError("Request", "Request modelbinding throw.");
                bindingContext.Result = ModelBindingResult.Failed();

                return Task.CompletedTask;
            }
        }
    }
}
