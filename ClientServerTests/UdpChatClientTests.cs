using ClientServer;
using ClientServer.Enums;
using ClientServer.Messages;
using ClientServerTests.Servers;

namespace ClientServerTests;

public class UdpChatClientTests {
  private readonly ChatSimulator _chatSimulator =
    new(new UdpServer("127.0.0.1", (ushort)(9999U + Utils.Counter.GetNext()), 1, 200));


  [Fact]
  public async Task UdpChatClient_TC1_StartStateInterruption_NoByeSent() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage(null) // nothing received
    );

    Assert.Equal([], _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC2_AuthState_ByeReceived() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ByeMessage(Id: 1)),
      () => _chatSimulator.ReceiveMessage("") // CONFIRM (Bye)
    );

    Assert.Equal([MsgType.Auth, MsgType.Confirm, MsgType.Bye, MsgType.Confirm],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC3_AuthState_ErrorReceived() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH:1
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ErrorMessage(DisplayName: "SERVER", Content: "ERROR",
        Id: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Error)
      () => _chatSimulator.ReceiveMessage(""), // BYE:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)),
      () => _chatSimulator.ReceiveMessage(null)); // nothing received

    Assert.Equal(
      [MsgType.Auth, MsgType.Confirm, MsgType.Err, MsgType.Confirm, MsgType.Bye, MsgType.Confirm],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC4_AuthState_InvalidMessageReceived() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH:1
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new InvalidMessage("INVALID MESSAGE", Id: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Invalid)
      () => _chatSimulator.ReceiveMessage(""), // ERR:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)),
      () => _chatSimulator.ReceiveMessage(""), // BYE:3
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 3)));

    Assert.Equal(
      [
        MsgType.Auth, MsgType.Confirm, MsgType.Invalid, MsgType.Confirm, MsgType.Err,
        MsgType.Confirm, MsgType.Bye, MsgType.Confirm
      ],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC5_AuthState_RetryAuth() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER-1 SECRET TEST_NAME"), // AUTH:1
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Nok, Content: "NOK",
        RefMsgId: 1, Id: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage("/auth USER-2 SECRET TEST_NAME"), // AUTH:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, Content: "OK",
        RefMsgId: 2, Id: 2)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage(null), // BYE:3
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 3)));

    Assert.Equal(
      [
        MsgType.Auth, MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Auth,
        MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Bye, MsgType.Confirm
      ],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC6_AuthState_SendBye() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER-1 SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Nok, Content: "NOK", Id: 1,
        RefMsgId: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage(null), // BYE:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)));

    Assert.Equal(
      [MsgType.Auth, MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Bye, MsgType.Confirm],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC7_AuthState_InvalidReplyResult_ErrorSent() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Invalid, Content: "OK")),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage(""), // ERR:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)),
      () => _chatSimulator.ReceiveMessage(""), // BYE:3
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 3)));

    Assert.Equal(
      [
        MsgType.Auth, MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Err, MsgType.Confirm,
        MsgType.Bye, MsgType.Confirm
      ],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC8_OpenState_ByeReceived() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH:1
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, Content: "OK",
        RefMsgId: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.SendMessage(new ByeMessage()),
      () => _chatSimulator.ReceiveMessage("")); // CONFIRM (Bye)

    Assert.Equal(
      [MsgType.Auth, MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Bye, MsgType.Confirm],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC9_OpenStateInterruption_SentBye() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH:1
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, Content: "OK",
        RefMsgId: 1, Id: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage(null), // BYE:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)));

    Assert.Equal(
      [MsgType.Auth, MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Bye, MsgType.Confirm],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC10_OpenState_ErrorReceived() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH:1
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, Content: "OK",
        RefMsgId: 1, Id: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.SendMessage(new ErrorMessage(DisplayName: "SERVER", Content: "ERROR",
        Id: 2)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Err)
      () => _chatSimulator.ReceiveMessage(""), // BYE:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)));

    Assert.Equal(
      [
        MsgType.Auth, MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Err, MsgType.Confirm,
        MsgType.Bye, MsgType.Confirm
      ],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC11_OpenState_InvalidMessage() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK", RefMsgId: 1, Id: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.SendMessage(new InvalidMessage(Content: "INVALID MESSAGE", Id: 2)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Invalid)
      () => _chatSimulator.ReceiveMessage(""), // ERR:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)),
      () => _chatSimulator.ReceiveMessage(""), // BYE:3
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 3)));

    Assert.Equal(
      [
        MsgType.Auth, MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Invalid,
        MsgType.Confirm, MsgType.Err, MsgType.Confirm, MsgType.Bye, MsgType.Confirm,
      ],
      _chatSimulator.ExchangedMessages);
  }


  [Fact]
  public async Task UdpChatClient_TC12_OpenState_MessageExchange() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH:1
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK", RefMsgId: 1, Id: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage("TEST MESSAGE 1"), // MSG:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)),
      () => _chatSimulator.SendMessage(new TextMessage("SERVER", "MESSAGE", Id: 2)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Msg)
      () => _chatSimulator.ReceiveMessage("TEST MESSAGE 2"), // MSG:3
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 3)),
      () => _chatSimulator.ReceiveMessage(null), // BYE
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 4)));

    Assert.Equal(
      [
        MsgType.Auth, MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Msg, MsgType.Confirm,
        MsgType.Msg, MsgType.Confirm, MsgType.Msg, MsgType.Confirm, MsgType.Bye, MsgType.Confirm
      ],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC13_OpenState_JoinSucceed() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH:1
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK", RefMsgId: 1, Id: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage("/join TEST-CHANNEL"), // JOIN:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK", RefMsgId: 2, Id: 2)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage("TEST MESSAGE"), // MSG:3
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 3)),
      () => _chatSimulator.ReceiveMessage(null), // BYE:4
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 4)));

    Assert.Equal(
      [
        MsgType.Auth, MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Join,
        MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Msg, MsgType.Confirm, MsgType.Bye,
        MsgType.Confirm
      ],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC14_OpenState_JoinFailed() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH:1
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK", RefMsgId: 1, Id: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage("/join TEST-CHANNEL"), // JOIN:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Nok, "NOK", RefMsgId: 2,
        Id: 2)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage("TEST MESSAGE"), // MSG:3
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 3)),
      () => _chatSimulator.ReceiveMessage(null), // BYE:4
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 4)));

    Assert.Equal(
      [
        MsgType.Auth, MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Join,
        MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Msg, MsgType.Confirm, MsgType.Bye,
        MsgType.Confirm
      ],
      _chatSimulator.ExchangedMessages);
  }

  [Fact]
  public async Task UdpChatClient_TC15_OpenState_InvalidReply() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH:1
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Ok, "OK", RefMsgId: 1, Id: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage("TEST MESSAGE"), // MSG:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Nok, "NOK", Id: 2)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage(""), // ERR:3
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 3)),
      () => _chatSimulator.ReceiveMessage(""), // BYE:4
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 4)));

    Assert.Equal(
      [
        MsgType.Auth, MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Msg, MsgType.Confirm,
        MsgType.Reply, MsgType.Confirm, MsgType.Err, MsgType.Confirm, MsgType.Bye, MsgType.Confirm
      ],
      _chatSimulator.ExchangedMessages);
  }


  [Fact]
  public async Task UdpChatClient_TC16_AuthState_InvalidReplyResult_ErrorSent() {
    await _chatSimulator.Simulate(
      () => _chatSimulator.ReceiveMessage("/auth USER SECRET TEST_NAME"), // AUTH:1
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 1)),
      () => _chatSimulator.SendMessage(new ReplyMessage(ReplyResult.Invalid, Content: "OK",
        RefMsgId: 1, Id: 1)),
      () => _chatSimulator.ReceiveMessage(""), // CONFIRM (Reply)
      () => _chatSimulator.ReceiveMessage(""), // ERR:2
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 2)),
      () => _chatSimulator.ReceiveMessage(""), // BYE:3
      () => _chatSimulator.SendMessage(new ConfirmMessage(Id: 3)));

    Assert.Equal(
      [
        MsgType.Auth, MsgType.Confirm, MsgType.Reply, MsgType.Confirm, MsgType.Err, MsgType.Confirm,
        MsgType.Bye, MsgType.Confirm
      ],
      _chatSimulator.ExchangedMessages);
  }
}