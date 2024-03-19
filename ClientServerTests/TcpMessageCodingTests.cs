using ClientServer.Enums;
using ClientServer.Messages;

namespace ClientServerTests;

public class TcpMessageCodingTests {
  public static IEnumerable<object[]> CommonTcpTestData() {
  // @formatter:off
    yield return [new AuthMessage("test", "secret", "Display_name"), "AUTH test AS Display_name USING secret\r\n"];
    yield return [new JoinMessage("channel", "Display_name"), "JOIN channel AS Display_name\r\n"];
    yield return [new ByeMessage(), "BYE\r\n"];
    yield return [new TextMessage(DisplayName: "Display_name", Content: "test message"),"MSG FROM Display_name IS test message\r\n"];
    yield return [new ErrorMessage(DisplayName: "Display_name", Content: "test message"), "ERR FROM Display_name IS test message\r\n"];
    yield return [new ReplyMessage(ReplyResult.Ok,  "Ok"), "REPLY OK IS Ok\r\n"];
    yield return [new ReplyMessage(ReplyResult.Nok,  "Nok"), "REPLY NOK IS Nok\r\n"];
    yield return [new InvalidMessage("INVALID MESSAGE"), "INVALID MESSAGE\r\n"];
    // @formatter:on
  }

  public static IEnumerable<object[]> FromTcp_TestData() {
    yield return [new InvalidMessage("INVALID MESSAGE"), "REPLY XXX IS Ok\r\n"];
    yield return [new ReplyMessage(ReplyResult.Nok,  "Nok"), "rePly nOk Is Nok\r\n"];
    yield return [new ErrorMessage(DisplayName: "Display_name", Content: "test message"), "ErR fROm Display_name iS test message\r\n"];
    yield return [new TextMessage(DisplayName: "Display_name", Content: "test message"),"MsG FroM Display_name Is test message\r\n"];
    yield return [new ByeMessage(), "Bye\r\n"];
    yield return [new JoinMessage("channel", "Display_name"), "jOIn channel As Display_name\r\n"];
    yield return [new AuthMessage("test", "secret", "Display_name"), "AuTh test aS Display_name UsiNG secret\r\n"];
  }


  [Theory]
  [MemberData(nameof(CommonTcpTestData))]
  public void ToTcpFormat_ShouldReturnCorrectString(Message message, string expected) {
    // Arrange & Act
    string result = message.ToTcpFormat();

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [MemberData(nameof(CommonTcpTestData))]
  [MemberData(nameof(FromTcp_TestData))]
  public void FromTcpFormat_ShouldReturnCorrectMessageType(Message expectedMessage,
    string tcpMessage) {
    // Act
    Message result = Message.FromTcpFormat(tcpMessage);

    // Assert
    Assert.Equal(expectedMessage, result);
  }
}