using ClientServer.Clients;

namespace ClientServer {
  public class Program {
    public static async Task Main(string[] args) {
      CommandLineParser.Options options = CommandLineParser.GetCommandLineArguments(args);

      BaseClient client = options.Client switch {
        "tcp" => new TcpClient(options.Hostname, (ushort)options.Port),
        "udp" => new UdpClient(options.Hostname, (ushort)options.Port, options.Timeout,
          options.Retransmissions),
        _ => throw new ArgumentException("Invalid client name"),
      };

      ChatClient chat = new(client);
      await chat.Communicate();
    }
  }
}