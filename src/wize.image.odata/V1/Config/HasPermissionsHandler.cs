using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wize.image.odata.V1.Config
{
    public class HasPermissionsHandler : AuthorizationHandler<HasPermissionsRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasPermissionsRequirement requirement)
        {
            if (!context.User.HasClaim(c => c.Type == "permissions" && c.Issuer == requirement.Issuer))
                return Task.CompletedTask;

            var permissions = context.User.FindAll(c => c.Type == "permissions" && c.Issuer == requirement.Issuer);

            if (permissions.Any(p => p.Value == requirement.Permissions))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
