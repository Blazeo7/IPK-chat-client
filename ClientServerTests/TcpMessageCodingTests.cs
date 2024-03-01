using ClientServer.Messages;

namespace ClientServerTests;

public class TcpMessageCodingTests {
  public static IEnumerable<object[]> TestDataTcp() {
  // @formatter:off
    yield return [new AuthMessage("name", "pass", "alias"), "AUTH name AS alias USING pass\r\n"];
    yield return [new JoinMessage("general", "alias"), "JOIN general AS alias\r\n"];
    yield return [new ByeMessage(), "BYE\r\n"];
    yield return [new TextMessage(displayName: "alias", content: "test message"),"MSG FROM alias IS test message\r\n"];
    yield return [new ErrorMessage(displayName: "alias", content: "test message"), "ERROR FROM alias IS test message\r\n"];
    // @formatter:on
  }


  [Theory]
  [MemberData(nameof(TestDataTcp))]
  public void ToTcpFormat_ShouldReturnCorrectString(Message message, string expected) {
    // Arrange & Act
    string result = message.ToTcpFormat();

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData("MSG FROM Alice IS Hello", typeof(TextMessage))]
  [InlineData("REPLY OK IS Success", typeof(ReplyMessage))]
  [InlineData("REPLY NOK IS Failure", typeof(ReplyMessage))]
  [InlineData("ERROR FROM Server IS Error Message", typeof(ErrorMessage))]
  [InlineData("BYE", typeof(ByeMessage))]
  [InlineData("InvalidMessageFormat", typeof(ErrorMessage))] // Example of an invalid message format
  public void FromTcpFormat_ShouldReturnCorrectMessageType(string tcpMessage, Type expectedType) {
    // Act
    Message result = Message.FromTcpFormat(tcpMessage);

    // Assert
    Assert.IsType(expectedType, result);
  }
}