using CommandLine;

namespace ClientServer;

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
  }

  public static Options GetCommandLineArguments(string[] args) {
    ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args);

    result.WithNotParsed((errors) => Environment.Exit(1));

    return result.Value;
  }
}