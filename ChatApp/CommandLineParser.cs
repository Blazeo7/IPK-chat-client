using CommandLine;

namespace ChatApp;

public class CommandLineParser {
  public class Options {
    [Option('t', Required = true, HelpText = "Transport protocol used for connection")]
    public required string Client { get; set; }

    [Option('s', Required = true, HelpText = "Server IP or hostname")]
    public required string Hostname { get; set; }

    [Option('p', Default = 4567, Required = false, HelpText = "Server port")]
    public int Port { get; set; }

    [Option('d', Default = 250, Required = false, HelpText = "UDP confirmation timeout (ms)")]
    public int Timeout { get; set; }

    [Option('r', Default = 3, Required = false,
      HelpText = "Maximum number of UDP retransmissions")]
    public int Retransmissions { get; set; }

    [Option('v', Required = false, HelpText = "Verbose (prints logs to stderr")]
    public bool Verbose { get; set; }

    [Option('h', HelpText = "Display this help screen.")]
    public bool ShowHelp { get; set; }
  }

  private static void HelpMessage() {
    Console.Error.WriteLine(
      """
            -t           Required. Transport protocol used for connection
      
            -s           Required. Server IP or hostname
      
            -p           (Default: 4567) Server port
      
            -d           (Default: 250) UDP confirmation timeout (ms)
      
            -r           (Default: 3) Maximum number of UDP retransmissions
      
            -v           Verbose (prints logs to stderr)
      
            -h           Display this help screen.
      """);
  }

  public static Options GetCommandLineArguments(string[] args) {
    ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args);

    result.WithNotParsed((_) => {
        Environment.Exit(1);
      }
    );

    if (result.Value.ShowHelp || (result.Value.Client != "tcp" && result.Value.Client != "udp")) {
      Utils.PrintInternalError("Invalid client name. Use either `tcp` or `udp` client");
      HelpMessage();
      Environment.Exit(1);
    }

    return result.Value;
  }
}