using System.Net;
using System.Net.Sockets;
using Testcontainers.RabbitMq;

namespace CommerceCore.TestSupport;

public sealed class RabbitMqBroker : IAsyncDisposable
{
    private readonly RabbitMqContainer _container;

    public RabbitMqBroker()
    {
        Port = GetFreePort();
        _container = new RabbitMqBuilder("rabbitmq:4")
            .WithUsername(Username)
            .WithPassword(Password)
            .WithPortBinding(Port, 5672)
            .Build();
    }

    public string Host => "localhost";
    public int Port { get; }
    public string Username => "test";
    public string Password => "test";

    public Task StartAsync() => _container.StartAsync();
    public Task StopAsync() => _container.StopAsync();
    public ValueTask DisposeAsync() => _container.DisposeAsync();

    // The services read these through ASP.NET Core's environment-variable configuration.
    public void ExportSettings()
    {
        Environment.SetEnvironmentVariable("RabbitMq__Host", Host);
        Environment.SetEnvironmentVariable("RabbitMq__Port", Port.ToString());
        Environment.SetEnvironmentVariable("RabbitMq__Username", Username);
        Environment.SetEnvironmentVariable("RabbitMq__Password", Password);
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
