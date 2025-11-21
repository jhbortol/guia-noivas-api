using Hangfire.Dashboard;

namespace GuiaNoivas.Api
{
    public class AllowAnonymousAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context) => true;
    }
}
