﻿using KVHAI.Hubs;

namespace KVHAI.Routes
{
    public class ResidentRoute
    {
        public static void RegisterRoute(IEndpointRouteBuilder endpoint)
        {
            endpoint.MapControllerRoute(
                name: "ResidentLogin",
                pattern: "kvhai/resident/login",
                defaults: new { controller = "ResLogin", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "ResidentSignup",
                pattern: "kvhai/resident/signup",
                defaults: new { controller = "ResLogin", action = "Signup" }
            );

            endpoint.MapControllerRoute(
                name: "AccountVerification",
                pattern: "kvhai/resident/verifyaccount/{token?}",
                defaults: new { controller = "ResLogin", action = "VerifyEmail" }
            );

            endpoint.MapControllerRoute(
               name: "AccountVerification",
               pattern: "kvhai/error",
               defaults: new { controller = "ResLogin", action = "VerifyPage" }
           );

            #region USER LOGGED IN TEMPLATE
            //LOGGEDIN HOME
            endpoint.MapControllerRoute(
                name: "ResidentAnnouncement",
                pattern: "kvhai/resident/announcement",
                defaults: new { controller = "LoggedIn", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: " ResidentRenterHome",
                pattern: "kvhai/resident/rental/home",
                defaults: new { controller = "ResidentHome", action = "Index" }
            );
            endpoint.MapControllerRoute(
                name: "ResidentOwnerHome",
                pattern: "kvhai/resident/rental-application",
                defaults: new { controller = "OwnerHome", action = "OwnerHome" }
            );

            endpoint.MapControllerRoute(
                name: "ResidentRegisterAddress",
                pattern: "kvhai/resident/register-address",
                defaults: new { controller = "LoggedIn", action = "LoggedIn" }
            );

            //MY ADDRESS
            endpoint.MapControllerRoute(
                name: "ResidentMyAddress",
                pattern: "kvhai/resident/my-address",
                defaults: new { controller = "MyAddress", action = "Index" }
            );

            //Renter ADDRESS
            endpoint.MapControllerRoute(
                name: "RenterAddress",
                pattern: "kvhai/resident/renter-address",
                defaults: new { controller = "RenterAddress", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "ResidentBilling",
                pattern: "kvhai/resident/billing",
                defaults: new { controller = "Billing", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "ResidentConsumption",
                pattern: "kvhai/resident/water-consumption",
                defaults: new { controller = "WaterConsumption", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "Settings",
                pattern: "kvhai/resident/account/settings",
                defaults: new { controller = "Settings", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "RenterRegisterAddress",
                pattern: "kvhai/resident/register/address",
                defaults: new { controller = "ResidentHome", action = "Index" }
            );

            endpoint.MapControllerRoute(
                name: "NotifactionMobile",
                pattern: "kvhai/resident/notification",
                defaults: new { controller = "NotificationMobile", action = "Index" }
            );

            #endregion

            #region FOR SIGNALR MAPS
            endpoint.MapHub<AnnouncementHub>("/resident/home");

            endpoint.MapHub<NotificationHub>("/resident/water-consumption");
            endpoint.MapHub<NotificationHub>("/resident/water-bill");
            endpoint.MapHub<NotificationHub>("/resident/register-address");
            endpoint.MapHub<NotificationHub>("/resident/my-address");
            endpoint.MapHub<AnnouncementHub>("/resident/announcement");

            endpoint.MapHub<NotificationHub>("/resident/notification");
            #endregion

        }
    }
}
