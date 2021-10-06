using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wize.image.odata.V1.Config
{
    public static class Authentication
    {
        public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration configuration)
        {
            JwtModel jwt = new JwtModel();
            jwt.ValidAudience = Environment.GetEnvironmentVariable("JwtAuthentication_ValidAudience");
            jwt.ValidIssuer = Environment.GetEnvironmentVariable("JwtAuthentication_ValidIssuer");

            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.IncludeErrorDetails = true;
                options.RequireHttpsMetadata = false;
                options.Authority = jwt.ValidIssuer;
                options.Audience = jwt.ValidAudience;
                //options.TokenValidationParameters = tokenParameters;
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("read:images", policy => policy.Requirements.Add(new HasScopeRequirement("read:images", jwt.ValidIssuer)));
            });

            return services;
        }

        public static IApplicationBuilder UseJwt(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            return app;
        }
    }
}
