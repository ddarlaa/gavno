using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace Migrations
{
    public static class MigrationRunner
    {
        public static void RunMigrations(string connectionString)
        {
            var serviceProvider = CreateServices(connectionString);

            using var scope = serviceProvider.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            // Выполнить миграции
            runner.MigrateUp();
        }

        private static ServiceProvider CreateServices(string connectionString)
        {
            return new ServiceCollection()
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(InitialCreate).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                .BuildServiceProvider(false);
        }
    }
}