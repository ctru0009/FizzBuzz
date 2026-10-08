using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace backend.Middleware
{
    public sealed class AntiforgeryExemptionConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            if (!action.Controller.ControllerName.Equals("Players", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var template = action.Selectors
                .Select(selector => selector.AttributeRouteModel?.Template)
                .FirstOrDefault(route => route is not null) ?? string.Empty;
            var isExemptRoute = template.EndsWith("/register", StringComparison.OrdinalIgnoreCase)
                || template.EndsWith("/login", StringComparison.OrdinalIgnoreCase)
                || template.EndsWith("/logout", StringComparison.OrdinalIgnoreCase);
            var isExemptAction = action.ActionName.Contains("Register", StringComparison.OrdinalIgnoreCase)
                || action.ActionName.Contains("Login", StringComparison.OrdinalIgnoreCase)
                || action.ActionName.Contains("Logout", StringComparison.OrdinalIgnoreCase);
            if (isExemptRoute || isExemptAction)
            {
                action.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
            }
        }
    }
}
