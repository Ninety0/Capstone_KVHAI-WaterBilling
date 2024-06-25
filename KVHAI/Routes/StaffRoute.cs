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
        }
    }
}
