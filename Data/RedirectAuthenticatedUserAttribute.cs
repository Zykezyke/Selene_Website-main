using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SELENE_STUDIO.Data
{
    public class RedirectAuthenticatedUserAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.Identity.IsAuthenticated)
            {
                var isAdmin = context.HttpContext.User.IsInRole("Admin");
                var routeValues = isAdmin ? new RouteValueDictionary(new { controller = "Admin", action = "Index" }) :
                                            new RouteValueDictionary(new { controller = "User", action = "Home" });
                context.Result = new RedirectToRouteResult(routeValues);
            }

            base.OnActionExecuting(context);
        }
    }
}
