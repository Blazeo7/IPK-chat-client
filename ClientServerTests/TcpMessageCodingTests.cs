using System.Text;
using System.Text.RegularExpressions;
using ClientServer.Messages;

namespace ClientServerTests;

public class TcpMessageCodingTests {
  public static IEnumerable<object[]> TestDataTcp() {
  // @formatter:off
    yield return [new AuthMessage("name", "pass", "alias"), "AUTH name AS alias USING pass\r\n"];
    yield return [new JoinMessage("general", "alias"), "JOIN general AS alias\r\n"];
    yield return [new ByeMessage(), "BYE\r\n"];
    yield return [new TextMessage(displayName: "alias", content: "test message"),"MSG FROM alias IS test message\r\n"];
    yield return [new ErrorMessage(displayName: "alias", content: "test message"), "ERR FROM alias IS test message\r\n"];
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

  [Fact]
  public void Test() {
    var arr = new byte[] { 82, 69, 80, 76, 89, 32, 79, 75, 32, 73, 83, 32, 79, 75, 13, 10, 0, 0 };
    string byteArrayAsString = Encoding.ASCII.GetString(arr).TrimEnd('\0');
    string regexPattern = @"^REPLY (OK|NOK) IS ([ -~]+)\r\n$";
    var x = Regex.Match(byteArrayAsString, regexPattern);
    Assert.True(x.Success);
  }

  [Theory]
  [InlineData("MSG FROM Alice IS Hello\r\n", typeof(TextMessage))]
  [InlineData("REPLY OK IS Success\r\n", typeof(ReplyMessage))]
  [InlineData("REPLY NOK IS Failure\r\n", typeof(ReplyMessage))]
  [InlineData("ERR FROM Server IS Error Message\r\n", typeof(ErrorMessage))]
  [InlineData("BYE\r\n", typeof(ByeMessage))]
  [InlineData("InvalidMessageFormat\r\n",
    typeof(ErrorMessage))] // Example of an invalid message format
  public void FromTcpFormat_ShouldReturnCorrectMessageType(string tcpMessage, Type expectedType) {
    // Act
    Message result = Message.FromTcpFormat(tcpMessage);

    // Assert
    Assert.IsType(expectedType, result);
  }
}