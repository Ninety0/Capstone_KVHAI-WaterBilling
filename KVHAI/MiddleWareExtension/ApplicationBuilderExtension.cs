using KVHAI.SubscribeSqlDependency;

namespace KVHAI.MiddleWareExtension
{
    public static class ApplicationBuilderExtension
    {
        public static void UseStreetTableDependency<T>(this IApplicationBuilder applicationBuilder) where T : ISubscribeTableDependency
        {
            using (var scope = applicationBuilder.ApplicationServices.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<SubscribeStreetTableDependency>();
                service.SubscribeTableDependency();

                //var serviceProvider = applicationBuilder.ApplicationServices;
                //var service = serviceProvider.GetService<SubscribeStreetTableDependency>();
                //service.SubscribeTableDependency();
            }

        }
    }
}
