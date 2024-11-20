using KVHAI.Hubs;

namespace KVHAI.Routes
{
    public class StaffRoute
    {
        public static void RegisterRoutes(IEndpointRouteBuilder endpoint)
        {
            endpoint.MapControllerRoute(
                name: "StaffLogin",
                pattern: "/kvhai/staff/login",
                defaults: new { controller = "AdminLogin", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "StaffError",
                pattern: "/kvhai/admin/error",
                defaults: new { controller = "AdminLogin", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "StaffForgotPass",
                pattern: "/kvhai/staff/login/forgot-password",
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
               pattern: "kvhai/staff/announcement/",
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
                name: "ClerkDashboard",
                pattern: "kvhai/staff/clerkhome/",
                defaults: new { controller = "Clerk", action = "Dashboard" }
            );

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


            #endregion

            /////////////////////////////////////////////////////////////////
            #region CASHIER ON
            endpoint.MapControllerRoute(
               name: "CashierOnlinePayment",
               pattern: "/kvhai/staff/onlinepayment/home",
               defaults: new { controller = "OnlinePayment", action = "Index" }
           );
            #endregion

            ////////////////////////////////////////////////////////////////
            #region CASHIER OFF
            endpoint.MapControllerRoute(
               name: "CashierOfflinePaymentHistory",
               pattern: "/kvhai/staff/offlinepayment/home",
               defaults: new { controller = "OfflinePayment", action = "History" }
           );

            endpoint.MapControllerRoute(
                name: "CashierOfflinePayment",
                pattern: "/kvhai/staff/offlinepayment/",
                defaults: new { controller = "OfflinePayment", action = "Index" }
            );
            #endregion

            ////////////////////////////////////////////////////////////////

            #region WATER WORKS
            endpoint.MapControllerRoute(
                name: "WaterWorksHome",
                pattern: "kvhai/staff/waterwork/home",
                defaults: new { controller = "WaterWorks", action = "Home" }
            );

            endpoint.MapControllerRoute(
                name: "WaterWorks",
                pattern: "kvhai/staff/waterwork/reading",
                defaults: new { controller = "WaterWorks", action = "Index" }
            );
            #endregion

            #region FOR SIGNALR MAPS
            endpoint.MapHub<StreetHub>("/kvhai/staff/admin/streethub");

            endpoint.MapHub<WaterReadingHub>("/kvhai/staff/readinghub");

            endpoint.MapHub<StaffNotificationHub>("/kvhai/staff/reading");

            endpoint.MapHub<StaffNotificationHub>("/staff/notification");
            endpoint.MapHub<StaffNotificationHub>("/staff/account");
            endpoint.MapHub<StaffNotificationHub>("/staff/register-address");
            endpoint.MapHub<StaffNotificationHub>("/staff/my-address");
            endpoint.MapHub<StaffNotificationHub>("/staff/waterbilling");
            endpoint.MapHub<StaffNotificationHub>("/staff/dashboard");

            #endregion
        }
    }
}
