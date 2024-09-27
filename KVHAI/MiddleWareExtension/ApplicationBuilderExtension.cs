using KVHAI.SubscribeSqlDependency;

namespace KVHAI.MiddleWareExtension
{
    public static class ApplicationBuilderExtension
    {
        public static void UseSqlTableDependency<T>(this IApplicationBuilder applicationBuilder, string connectionString) where T : ISubscribeTableDependency
        {
            var serviceProvider = applicationBuilder.ApplicationServices;
            using (var scope = serviceProvider.CreateScope()) // Create a scope manually
            {
                var service = scope.ServiceProvider.GetRequiredService<T>(); // Resolve scoped service
                service.SubscribeTableDependency(connectionString);
            }
        }

    }
}
