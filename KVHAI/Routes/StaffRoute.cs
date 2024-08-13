using KVHAI.Hubs;

namespace KVHAI.Routes
{
    public class StaffRoute
    {
        public static void RegisterRoutes(IEndpointRouteBuilder endpoint)
        {
            endpoint.MapControllerRoute(
                name: "StaffLogin",
                pattern: "kvhai/staff/login",
                defaults: new { controller = "Login", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "AdminDashboard",
                pattern: "kvhai/staff/admin/dashboard",
                defaults: new { controller = "AdminDashboard", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "AdminAccounts",
                pattern: "kvhai/staff/admin/accounts",
                defaults: new { controller = "AdminAccount", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "AdminStreets",
                pattern: "kvhai/staff/admin/streets",
                defaults: new { controller = "Street", action = "Index" }
            );

            endpoint.MapHub<StreetHub>("/kvhai/staff/admin/streethub");

            endpoint.MapControllerRoute(
                name: "BillingClerk",
                pattern: "kvhai/staff/water-billing/",
                defaults: new { controller = "Clerk", action = "Index" }
            );

            endpoint.MapHub<WaterReadingHub>("/kvhai/staff/readinghub");


            endpoint.MapControllerRoute(
                name: "WaterWorks",
                pattern: "kvhai/staff/waterwork/",
                defaults: new { controller = "WaterWorks", action = "Index" }
            );
        }
    }
}
