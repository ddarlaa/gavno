// using Microsoft.OpenApi;
// using Swashbuckle.AspNetCore.SwaggerGen;
//
// namespace IceBreakerApp.Application.Services;
//
// public class SwaggerStreamingFileFilter : IOperationFilter
// {
//     public void Apply(OpenApiOperation operation, OperationFilterContext context)
//     {
//         if (context.MethodInfo.Name == "UploadStream")
//         {
//             operation.RequestBody = new OpenApiRequestBody
//             {
//                 Content = new Dictionary<string, OpenApiMediaType>
//                 {
//                     ["multipart/form-data"] = new OpenApiMediaType
//                     {
//                         Schema = new OpenApiSchema
//                         {
//                             Type = "object",
//                             Properties = new Dictionary<string, OpenApiSchema>
//                             {
//                                 ["file"] = new OpenApiSchema
//                                 {
//                                     Description = "Select file (Streaming)",
//                                     Type = "string",
//                                     Format = "binary"
//                                 },
//                                 ["IsPublic"] = new OpenApiSchema
//                                 {
//                                     Type = "boolean"
//                                 },
//                                 ["ExpiresAt"] = new OpenApiSchema
//                                 {
//                                     Description = "Expires at",
//                                     Type = "string",
//                                     Format = "date-time"
//                                 }
//                             },
//                             Required = new HashSet<string> { "file" }
//                         }
//                     }
//                 }
//             };
//             
//             // Устанавливаем Default для IsPublic отдельно
//             ((OpenApiBoolean)operation.RequestBody.Content["multipart/form-data"]
//                 .Schema.Properties["IsPublic"].Default) = new OpenApiBoolean(false);
//         }
//     }
// }