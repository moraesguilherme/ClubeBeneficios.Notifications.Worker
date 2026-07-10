using System.Data;

namespace ClubeBeneficios.Notifications.Worker.Infrastructure.Database;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
