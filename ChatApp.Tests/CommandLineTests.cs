using ChatApp;
using CommandLine;

namespace ChatApp.Tests;

public class CommandLineTests {
  [Theory,
   InlineData(""),
   InlineData("-t", "tcp"), // missing required -s
   InlineData("-s", "localhost"), // missing required -t
   InlineData("-s", "localhost", "-t", "udp", "-p", "invalid port"), // invalid port number
  ]
  public void InvalidArgsTest(params string[] args) {
    // Act
    var parseResult = Parser.Default.ParseArguments<CommandLineParser.Options>(args)
      .WithNotParsed(errors => { });

    // Assert
    Assert.True(parseResult.Tag == ParserResultType.NotParsed);
  }


  [Fact]
  public void CorrectArgsTest() {
    // Arrange
    string[] args = ["-t", "tcp", "-s", "localhost"];

    // Act
    var parseResult = Parser.Default.ParseArguments<CommandLineParser.Options>(args)
      .WithNotParsed(errors => { });

    // Assert
    Assert.True(parseResult.Tag == ParserResultType.Parsed);
    Assert.Equal("tcp", parseResult.Value.Client);
    Assert.Equal("localhost", parseResult.Value.Hostname);
    Assert.Equal(4567, parseResult.Value.Port);
    Assert.Equal(250, parseResult.Value.Timeout);
    Assert.Equal(3, parseResult.Value.Retransmissions);
  }
}