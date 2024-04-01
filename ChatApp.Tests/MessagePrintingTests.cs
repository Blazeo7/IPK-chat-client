using ChatApp.Enums;
using ChatApp.Messages;

namespace ChatApp.Tests;

public class MessagePrintingTests {
  public static IEnumerable<object[]> TestPrintToStderr() {
  // @formatter:off
    yield return [new ErrorMessage(DisplayName: "TestUser", Content: "test message"), "ERR FROM TestUser: test message"];
    yield return [new ReplyMessage(ReplyResult.Ok, Content: "OK"), "Success: OK"];
    yield return [new ReplyMessage(ReplyResult.Nok, Content: "NOK"), "Failure: NOK"];
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