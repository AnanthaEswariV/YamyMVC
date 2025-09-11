using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace YamyProject.Filters
{
    public class AjaxOnlyAttribute : ActionMethodSelectorAttribute
    {
        public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
        {
         var request=routeContext.HttpContext.Request;
            var isAjax = request.Headers[""]=="";
            return isAjax;
        }
    }
}
