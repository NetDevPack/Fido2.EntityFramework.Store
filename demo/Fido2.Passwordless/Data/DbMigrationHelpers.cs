namespace Fido2.Passwordless.Data
{
    public static class DbMigrationHelpers
    {
        /// <summary>
        /// Generate migrations before running this method, you can use command bellow:
        /// Nuget package manager: Add-Migration DbInit -context ApplicationDbContext
        /// Dotnet CLI: dotnet ef migrations add DbInit -c ApplicationDbContext
        /// </summary>
        public static async Task EnsureSeedData(WebApplication app)
        {
            var services = app.Services.CreateScope().ServiceProvider;
            await EnsureSeedData(services);
        }

        public static async Task EnsureSeedData(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            var appContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();


            if (env.IsDevelopment())
                await appContext.Database.EnsureCreatedAsync();
        }

    }
}
