using ClientServer.Messages;

namespace ClientServerTests;

public class YourClassTests {
  public static IEnumerable<object[]> TestPrintToStderr() {
  // @formatter:off
    yield return [new ErrorMessage(displayName: "TestUser", content: "test message"), "ERROR FROM TestUser: test message"];
    yield return [new ErrorMessage(content: "test message"), "ERROR: test message"];
    yield return [new ReplyMessage(1, content: "OK"), "Success: OK"];
    yield return [new ReplyMessage(0, content: "NOK"), "Failure: NOK"];
    // @formatter:on
  }

  [Theory]
  [MemberData(nameof(TestPrintToStderr))]
  public void MessagePrint_ShouldPrintCorrectOutputToStderr(Message message, string expectedOutput) {
    // Arrange
    var stringWriter = new StringWriter();
    Console.SetError(stringWriter);

    // Act
    message.Print();

    // Assert
    string capturedOutput = stringWriter.ToString().Trim();
    Assert.Equal(expectedOutput, capturedOutput);
  }

  [Fact]
  public void MessagePrint_ShouldPrintCorrectOutputToStdout() {
    // Arrange
    Message message = new TextMessage("TestUser", "test content");
    var stringWriter = new StringWriter();
    Console.SetOut(stringWriter);

    // Act
    message.Print();

    // Assert
    string capturedOutput = stringWriter.ToString().Trim();
    Assert.Equal("TestUser: test content", capturedOutput);
  }
}