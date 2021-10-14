using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace wize.image.odata.V1.Config
{
    public static class OpenAPI
    {
        public static IServiceCollection AddOpenAPI(this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, OpenAPIOptions>();
            services.AddSwaggerGen(options =>
            {
                //options.ResolveConflictingActions(a => a.First());
                options.OperationFilter<OpenAPIParameters>();
                options.IgnoreObsoleteProperties();
                options.CustomSchemaIds(s => s.FullName);
            });

            return services;
        }

        public static IApplicationBuilder UseOpenAPI(this IApplicationBuilder app, IApiVersionDescriptionProvider provider)
        {
            //app.UseSwagger(options =>
            //{
            //    options.RouteTemplate
            //});
            app.UseSwaggerUI(options =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                    {
                        options.SwaggerEndpoint($"{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                        options.EnableDeepLinking();
                    }
                    else
                    {
                        //options.RoutePrefix = "settings-odata/swagger";
                        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                        options.EnableDeepLinking();
                    }
                    //options.RoutePrefix = string.Empty;
                }
            });

            return app;
        }
    }
}
