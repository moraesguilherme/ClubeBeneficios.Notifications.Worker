using System.Data;
using Microsoft.Data.SqlClient;

namespace ClubeBeneficios.Notifications.Worker.Infrastructure.Database;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection") ??
            _configuration.GetConnectionString("ClubeBeneficiosDb") ??
            throw new InvalidOperationException("Connection string 'DefaultConnection' ou 'ClubeBeneficiosDb' não encontrada.");

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return connection;
    }
}
