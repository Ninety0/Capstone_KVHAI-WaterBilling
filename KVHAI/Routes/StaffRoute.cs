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
                defaults: new { controller = "AdminLogin", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "StaffError",
                pattern: "/kvhai/admin/error",
                defaults: new { controller = "AdminLogin", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "StaffForgotPass",
                pattern: "kvhai/staff/login/forgot-password",
                defaults: new { controller = "AdminLogin", action = "Forgot" }
            );

            #region ADMIN
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

            endpoint.MapControllerRoute(
               name: "PostAnnouncement",
               pattern: "kvhai/staff/announcment/",
               defaults: new { controller = "PostAnnouncement", action = "Index" }
           );

            endpoint.MapControllerRoute(
                name: "ResidentConfirmation",
                pattern: "kvhai/staff/resident-address/",
                defaults: new { controller = "ResidentAddress", action = "Index" }
            );
            endpoint.MapControllerRoute(
                name: "PageRequest",
                pattern: "kvhai/staff/request-page/",
                defaults: new { controller = "RequestPage", action = "Index" }
            );
            #endregion
            #region CLERK
            endpoint.MapControllerRoute(
                name: "ReadingClerk",
                pattern: "kvhai/staff/water-reading/",
                defaults: new { controller = "Clerk", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "BillingClerk",
                pattern: "kvhai/staff/water-billing/",
                defaults: new { controller = "ClerkWaterBilling", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "WaterWorks",
                pattern: "kvhai/staff/waterwork/",
                defaults: new { controller = "WaterWorks", action = "Index" }
            );
            #endregion

            /////////////////////////////////////////////////////////////////
            #region CASHIER ON
            #endregion

            ////////////////////////////////////////////////////////////////
            #region CASHIER OFF
            #endregion

            ////////////////////////////////////////////////////////////////

            #region WATER WORKS
            #endregion

            #region FOR SIGNALR MAPS
            endpoint.MapHub<StreetHub>("/kvhai/staff/admin/streethub");

            endpoint.MapHub<WaterReadingHub>("/kvhai/staff/readinghub");

            #endregion
        }
    }
}
