using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using Testcontainers.PostgreSql;

namespace SpotOps.Tests.Integrations.Features.Events.Reserve;

public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    public AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        var db = new AppDbContext(options);
        db.Database.Migrate();
        return db;
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
