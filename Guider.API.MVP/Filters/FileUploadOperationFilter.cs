using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace Guider.API.MVP.Filters
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var methodInfo = context.MethodInfo;

            if (!context.ApiDescription.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                !context.ApiDescription.HttpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase) &&
                !context.ApiDescription.HttpMethod.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
                return;

            // Проверяем есть ли в методе параметры с типом IFormFile
            var fileParameters = methodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) ||
                           (p.ParameterType.IsGenericType && p.ParameterType.GetGenericArguments().Contains(typeof(IFormFile))));

            if (fileParameters.Any())
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content =
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            //Schema = new OpenApiSchema
                            //{
                            //    Type = "object",
                            //    Properties = context.SchemaRepository.Schemas.ContainsKey(context.SchemaGenerator.GenerateSchema(context.MethodInfo.GetParameters()[0].ParameterType, context.SchemaRepository).Reference.Id)
                            //        ? null
                            //        : fileParameters.ToDictionary(
                            //            k => k.Name,
                            //            v => new OpenApiSchema
                            //            {
                            //                Type = "string",
                            //                Format = "binary"
                            //            })
                            //}

                            Schema = context.SchemaGenerator
                            .GenerateSchema(methodInfo.GetParameters()[0].ParameterType,
                                        context.SchemaRepository)
                        }
                    }
                };
            }
        }
    }
}
