using ChatApp;
using ChatApp.Enums;
using ChatApp.Messages;
using ChatApp.Tests.Servers;

namespace ChatApp.Tests;

public class TcpChatClientTests {
  private readonly ChatSimulator _chatSimulator =
    new(new TcpServer("127.0.0.1", (ushort)(9999U + Utils.Counter.GetNext())));

  [Fact]
  public async Task TcpChatClient_TC1_StartStateInterruption_NoByeSent() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage(null) // nothing received
    );

    Assert.Equal([], _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC2_AuthState_ByeReceived() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ByeMessage()),
      () => _chatSimulator.ReceiveMessage(""), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal([MsgType.Auth, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC3_AuthState_ErrorReceived() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ErrorMessage(DisplayName: "SERVER", Content: "ERROR")),
      () => _chatSimulator.ReceiveMessage(""), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal([MsgType.Auth, MsgType.Err, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC4_AuthState_InvalidMessageReceived() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new InvalidMessage("INVALID MESSAGE")),
      () => _chatSimulator.ReceiveMessage(""), // ERR
      () => _chatSimulator.ReceiveMessage(""), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal(
      [MsgType.Auth, MsgType.Invalid, MsgType.Err, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC5_AuthState_RetryAuth() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER-1 SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Nok, Content: "NOK")),
      () => _chatSimulator.ReceiveMessage("/auth USER-2 SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, Content: "OK")),
      () => _chatSimulator.ReceiveMessage(null), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal(
      [MsgType.Auth, MsgType.Reply, MsgType.Auth, MsgType.Reply, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC6_AuthState_SendBye() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER-1 SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Nok, Content: "NOK")),
      () => _chatSimulator.ReceiveMessage(null), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal(
      [MsgType.Auth, MsgType.Reply, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC7_AuthState_InvalidReplyResult_ErrorSent() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Invalid, Content: "OK")),
      () => _chatSimulator.ReceiveMessage(""), // ERR
      () => _chatSimulator.ReceiveMessage(""), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing

    Assert.Equal([MsgType.Auth, MsgType.Reply, MsgType.Err, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC8_OpenState_ByeReceived() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, Content: "OK")),
      () => _chatSimulator.SendMessage(new ByeMessage()),
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal(
      [MsgType.Auth, MsgType.Reply, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC9_OpenStateInterruption_SentBye() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, Content: "OK")),
      () => _chatSimulator.ReceiveMessage(null), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal(
      [MsgType.Auth, MsgType.Reply, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC10_OpenState_ErrorReceived() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, Content: "OK")),
      () => _chatSimulator.SendMessage(new ErrorMessage(DisplayName: "SERVER", Content: "ERROR")),
      () => _chatSimulator.ReceiveMessage(""), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal(
      [MsgType.Auth, MsgType.Reply, MsgType.Err, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC11_OpenState_InvalidMessage() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK")),
      () => _chatSimulator.SendMessage(new InvalidMessage(Content: "INVALID MESSAGE")),
      () => _chatSimulator.ReceiveMessage(""), // ERR
      () => _chatSimulator.ReceiveMessage(""), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal([MsgType.Auth, MsgType.Reply, MsgType.Invalid, MsgType.Err, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }


  [Fact]
  public async Task TcpChatClient_TC12_OpenState_MessageExchange() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK")),
      () => _chatSimulator.ReceiveMessage("TEST MESSAGE 1"), // MSG
      () => _chatSimulator.SendMessage(new TextMessage("SERVER", "MESSAGE")),
      () => _chatSimulator.ReceiveMessage("TEST MESSAGE 2"), // MSG
      () => _chatSimulator.ReceiveMessage(null), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal([MsgType.Auth, MsgType.Reply, MsgType.Msg, MsgType.Msg, MsgType.Msg, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC13_OpenState_JoinSucceed() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK")),
      () => _chatSimulator.ReceiveMessage("/join TEST-CHANNEL"),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK")),
      () => _chatSimulator.ReceiveMessage("TEST MESSAGE"), // MSG
      () => _chatSimulator.ReceiveMessage(null), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal(
      [MsgType.Auth, MsgType.Reply, MsgType.Join, MsgType.Reply, MsgType.Msg, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC14_OpenState_JoinFailed() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK")),
      () => _chatSimulator.ReceiveMessage("/join TEST-CHANNEL"),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "NOK")),
      () => _chatSimulator.ReceiveMessage("TEST MESSAGE"), // MSG
      () => _chatSimulator.ReceiveMessage(null), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal(
      [MsgType.Auth, MsgType.Reply, MsgType.Join, MsgType.Reply, MsgType.Msg, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC15_OpenState_InvalidReply() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK")),
      () => _chatSimulator.ReceiveMessage("TEST MESSAGE"), // MSG
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Nok, "NOK")),
      () => _chatSimulator.ReceiveMessage(""), // ERR
      () => _chatSimulator.ReceiveMessage(""), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal(
      [MsgType.Auth, MsgType.Reply, MsgType.Msg, MsgType.Reply, MsgType.Err, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task TcpChatClient_TC16_ErrorState_NoMessageSent() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK")),
      () => _chatSimulator.SendMessage(new InvalidMessage(Content: "INVALID MESSAGE")),
      () => _chatSimulator.ReceiveMessage(""), // ERR
      () => _chatSimulator.ReceiveMessage("DO NOT SEND THIS MESSAGE"), // BYE
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal([MsgType.Auth, MsgType.Reply, MsgType.Invalid, MsgType.Err, MsgType.Bye],
      _chatSimulator.ExchangedMessages);
  }
}