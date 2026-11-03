using System.Net.Sockets;
using Microsoft.Data.SqlClient;

namespace OrderInventory.Api.Hosting;

public static class HealthProbe
{
    public static async Task<int> RunAsync()
    {
        try
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<Program>(optional: true).AddEnvironmentVariables().Build();
            var connectionString = configuration.GetConnectionString("Database");
            if (string.IsNullOrWhiteSpace(connectionString)) return 1;
            var portValue = configuration["ASPNETCORE_HTTP_PORTS"]?.Split(';')[0] ?? "8080";
            if (!int.TryParse(portValue, out var port)) return 1;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            using var socket = new TcpClient();
            await socket.ConnectAsync("127.0.0.1", port, timeout.Token);
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(timeout.Token);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 3;
            await command.ExecuteScalarAsync(timeout.Token);
            return 0;
        }
        catch (Exception)
        {
            return 1;
        }
    }
}
