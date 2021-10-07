using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wize.common.tenancy;
using wize.common.tenancy.Interfaces;
using wize.common.tenancy.Providers;
using wize.image.data.V1;
using wize.image.odata.V1.Config;
using wize.image.odata.V1.ModelConfigurations;

namespace wize.image.odata
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options => options.EnableEndpointRouting = false);
            services.AddApiVersioning(options => options.ReportApiVersions = true);
            services.AddJwt(Configuration);
            services.AddODataMvc();
            services.AddOpenAPI();
            services.AddHttpContextAccessor();
            services.AddTransient<ITenantProvider, TenantDatabaseProvider>();
            services.AddDbContext<ImageContext>(options =>
            {
                options.UseSqlServer(Configuration.GetValue<string>("ConnectionStrings_WizeWorksContext"));
            });
            services.AddDbContext<TenantContext>(options =>
            {
                options.UseSqlServer(Configuration.GetValue<string>("ConnectionStrings_TenantsContext"));
            });
            services.AddApplicationInsightsTelemetry(Configuration.GetValue<string>("ApplicationInsights_ConnectionString"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider, VersionedODataModelBuilder builder)
        {
            if (env.IsDevelopment())
            {
                //app.UseDeveloperExceptionPage();
            }
            app.UseStaticFiles();
            //app.UseHttpsRedirection();

            app.UseRouting();
            app.UseSentryTracing();

            app.UseJwt();
            app.UseOpenAPI(provider);
            app.UseODataMvc(builder);

        }
    }
}
