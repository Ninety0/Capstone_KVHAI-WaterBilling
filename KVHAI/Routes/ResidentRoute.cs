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

        }
    }
}
