using ClientServer.Enums;
using ClientServer.Messages;

namespace ClientServerTests;

public class UdpMessageCoding {
  public static IEnumerable<object[]> CommonUdpTestData() {
    // @formatter:off
    yield return [new AuthMessage("name", "pass", "alias", 1), 
                  new byte[] {0x2, 0x0, 0x1, 0x6E, 0x61, 0x6D, 0x65, 0x0,  0x61, 0x6C, 0x69, 0x61, 0x73, 0x0, 0x70, 0x61, 0x73, 0x73,0x0}];
    yield return [new JoinMessage("general", "alias", 2),
                  new byte[] {0x3, 0x0, 0x2,0x67, 0x65, 0x6E, 0x65, 0x72, 0x61, 0x6C, 0x0,0x61, 0x6C, 0x69, 0x61, 0x73, 0x0}];
    yield return [new ByeMessage(3), 
                  new byte[] { 0xFF, 0x0, 0x3 }];
    yield return [new TextMessage(DisplayName: "alias", Content: "test msg", Id: 4),
                  new byte[] {0x4, 0x0, 0x4, 0x61, 0x6C, 0x69, 0x61, 0x73, 0x0, 0x74, 0x65, 0x73,0x74, 0x20, 0x6D, 0x73, 0x67, 0x0}];
    yield return [new ReplyMessage(Result: ReplyResult.Ok, Content:"OK", 2, 3),
                  new byte[] {0x01, 0x0, 0x2, 0x1, 0x0, 0x3, 0x4F, 0x4B, 0x0}];
    yield return [new ErrorMessage(DisplayName: "alias", Content: "test msg", Id: 5),
                  new byte[] {0xFE, 0x0, 0x5, 0x61, 0x6C, 0x69, 0x61, 0x73, 0x0, 0x74, 0x65, 0x73, 0x74, 0x20, 0x6D, 0x73, 0x67, 0x0}];
    yield return [new ConfirmMessage(256), 
                  new byte[] { 0x0, 0x1, 0x0 }];
    // @formatter:on
  }

  public static IEnumerable<object[]> FromUdp_TestData() {
    // @formatter:off
    yield return [new InvalidMessage("Invalid reply message", 7),  // Invalid result
                  new byte[] {0x1, 0x0, 0x7, 0x99, 0x02, 0x00, 0x65, 0x0,  0x4F, 0x4B, 0x0}];
    // @formatter:on
  }

  [Theory]
  [MemberData(nameof(CommonUdpTestData))]
  public void ToUdpFormat_ShouldReturnCorrectString(Message message, byte[] expected) {
    // Arrange & Act
    byte[] result = message.ToUdpFormat();

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [MemberData(nameof(CommonUdpTestData))]
  [MemberData(nameof(FromUdp_TestData))]
  public void FromUdpFormat_ShouldReturnCorrectMessageType(Message expectedMessage,
    byte[] udpMessage) {
    // Act
    Message result = Message.FromUdpFormat(udpMessage);

    // Assert
    Assert.Equal(expectedMessage, result);
  }
}