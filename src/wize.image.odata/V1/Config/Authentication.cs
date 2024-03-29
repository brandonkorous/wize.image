﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace wize.image.odata.V1.Config
{
    public static class Authentication
    {
        public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration configuration)
        {
            JwtModel jwt = new JwtModel();
            jwt.ValidAudience = configuration.GetValue<string>("JwtAuthentication_ValidAudience");
            jwt.ValidIssuer = configuration.GetValue<string>("JwtAuthentication_ValidIssuer");

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
                options.AddPolicy("read:image", policy => policy.Requirements.Add(new HasPermissionsRequirement("read:image", jwt.ValidIssuer)));
                options.AddPolicy("add:image", policy => policy.Requirements.Add(new HasPermissionsRequirement("add:image", jwt.ValidIssuer)));
                options.AddPolicy("list:image", policy => policy.Requirements.Add(new HasPermissionsRequirement("list:image", jwt.ValidIssuer)));
                options.AddPolicy("update:image", policy => policy.Requirements.Add(new HasPermissionsRequirement("update:image", jwt.ValidIssuer)));
                options.AddPolicy("delete:image", policy => policy.Requirements.Add(new HasPermissionsRequirement("delete:image", jwt.ValidIssuer)));
            });
            services.AddSingleton<IAuthorizationHandler, HasPermissionsHandler>();

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
