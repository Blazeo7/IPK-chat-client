using System.Diagnostics;
using System.Net.Sockets;
using ChatApp.Clients;
using ChatApp.Enums;
using ChatApp.Messages;

namespace ChatApp;

/// <summary>
/// Represents a class responsible for handling communication with server.
/// </summary>
public class ChatClient {
  private IClient Client { get; }
  private readonly ChatData _chatInfo = new();
  private readonly UserCommandHandler _commandHandler;
  private readonly AutoResetEvent _replyAccessEvent = new(false);
  private readonly AutoResetEvent _leaveAccessEvent = new(true);

  public ChatClient(IClient client) {
    Client = client;
    _commandHandler = new UserCommandHandler(_chatInfo);
  }


  /// <summary>Leads communication from set up phase until the disconnection from server</summary>
  public async Task Communicate() {
    Client.SetUpConnection();
    _chatInfo.Connected = true;
    // handle interruption signal Ctrl+C
    Console.CancelKeyPress += async (_, e) => {
      // Prevent the default behavior (program termination)
      e.Cancel = true;
      Logger.Log("Ctrl+C");
      // Execute the LeaveChat function
      await LeaveChat();
    };

    try {
      while (_chatInfo.CurrentState != State.End) {
        await (_chatInfo.CurrentState switch {
          State.Start => StartState(),
          State.Auth => AuthState(),
          State.Open => OpenState(),
          State.Error => ErrorState(),
          _ => throw new UnreachableException("Invalid state"),
        });
      }
    } catch (SocketException e) {
      Logger.Log("Socket exception: " + e.Message);
      await LeaveChat();
    }
  }


  /// <summary>
  /// Allows user to send the authentication message. If the message was sent the state will be
  /// changed to <see cref="State.Auth"/>. 
  /// </summary>
  private async Task StartState() {
    var message = await GetUserInput();

    if (message.MType is MsgType.Auth) {
        bool isSuccess = await Client.SendMessage(message);

        if (isSuccess) {
          _chatInfo.CurrentState = State.Auth;
        } else {
        Logger.Log("Message was not confirmed");
          await LeaveChat();
        }
    } else {
        Utils.PrintInternalError("You need to authenticate using valid /auth command");
    }
  }


  /// <summary>
  /// In this state user awaits a reply message from server.
  /// <para/>
  /// State changes based on the message from server:
  /// <list type="bullet">
  ///     <item><see cref="ErrorMessage"/>: <see cref="State.End"/> and leave chat gracefully.</item>
  ///     <item><see cref="ReplyMessage"/> (NOK): <see cref="State.Start"/> and the received failure message is printed to Stderr</item>
  ///     <item><see cref="ReplyMessage"/> (OK): <see cref="State.Open"/> and the received success message is printed to Stderr</item>
  ///     <item>Otherwise: <see cref="State.Error"/> and an error message is printed to stderr</item>
  /// </list>
  /// </summary>
  private async Task AuthState() {
    var receiveMessage = await Client.ReceiveMessage();

    switch (receiveMessage.MType) {
      case MsgType.Reply:
        // REPLY OK => State = Open 
        // REPLY NOK => State = Start
        ReplyResult result = ((ReplyMessage)receiveMessage).Result;

        switch (result) {
          case ReplyResult.Ok:
            _chatInfo.CurrentState = State.Open;
            receiveMessage.Print();
            break;

          case ReplyResult.Nok:
            receiveMessage.Print();
            await StartState();
            break;

          case ReplyResult.Invalid:
            await TransitionToErrorState();
            break;

          default:
            throw new ArgumentOutOfRangeException();
        }

        break;


      case MsgType.Err:
        receiveMessage.Print();
        await LeaveChat();
        break;

      case MsgType.Bye:
        Client.EndConnection();
        _chatInfo.CurrentState = State.End;
        break;

      default:
        await TransitionToErrorState();
        break;
    }
  }

  /// <summary>
  /// Prints and sends an error message to the server. Sets state to <see cref="State.Error"/>
  /// </summary>
  private async Task TransitionToErrorState() {
    Utils.PrintInternalError("Invalid message from the server");

    Message error = new ErrorMessage("Got invalid message", DisplayName: _chatInfo.DisplayName,
      Id: Utils.Counter.GetNext());

    await Client.SendMessage(error);
    _chatInfo.CurrentState = State.Error;
  }

  /// <summary>
  /// Allows user to send and receive messages from server at the same time. Move to next state
  /// is only possible when <see cref="OpenStateReceiving"/> task ends.
  /// </summary>
  private async Task OpenState() {
    // Start two tasks
    Task receivingTask = Task.Run(OpenStateReceiving);
    Task sendingTask = Task.Run(OpenStateSending);

    // Wait for any task to complete
    await Task.WhenAny(receivingTask, sendingTask);
  }


  /// <summary>
  /// End client connection and exit application
  /// </summary>
  private async Task ErrorState() {
    await LeaveChat();
  }

  /// <summary>
  /// Gracefully leaves chat and end connection with server
  /// </summary>
  private async Task LeaveChat() {
    // avoid calling this function multiple times from each thread

    await Task.Run(_leaveAccessEvent.WaitOne);
    if (_chatInfo.Connected) {
      _chatInfo.Connected = false;

      if (_chatInfo.CurrentState != State.Start) {
        Logger.Log("Leaving");
        await Client.SendMessage(new ByeMessage(Id: Utils.Counter.GetNext()));
      }

      Client.EndConnection();
      _replyAccessEvent.Dispose();
      _leaveAccessEvent.Dispose();
      Logger.Log("Exit");
      Environment.Exit(0);
    }
  }

  /// <summary>
  /// Allow sending messages to server nonstop until it is not killed by other the thread.
  /// </summary>
  private async Task OpenStateSending() {
    while (true) {
      Message message = await GetUserInput();

      if (_chatInfo.CurrentState != State.Open) {
        Logger.Log("Invalid state to sending message");
        return;
      }

      switch (message.MType) {
        case MsgType.Msg:
          bool isMsgSent = await Client.SendMessage(message);

          if (!isMsgSent) { // message was not received by the server
            await LeaveChat();
          }

          break;

        case MsgType.Join:
          bool isJoinSent = await Client.SendMessage(message);

          if (!isJoinSent) { // message was not received by the server
            await LeaveChat();
          }

          _chatInfo.ReplyExpected = true;
          await Task.Run(_replyAccessEvent.WaitOne);
          break;

        default:
          Utils.PrintInternalError("Invalid message read from the input");
          break;
      }
    }
  }

  /// <summary>
  /// Allows user asynchronously receiving text messages from the server until. Server subscribing
  /// is interrupted when received message is not TextMessage.
  /// </summary>
  private async Task OpenStateReceiving() {
    try {
      while (true) {
        Message message = await Client.ReceiveMessage();

        switch (message.MType) {
          case MsgType.Msg:
            message.Print();
            break;

          case MsgType.Reply:
            if (!_chatInfo.ReplyExpected) {
              await TransitionToErrorState();
              return;
            }

            _chatInfo.ReplyExpected = false;
            _replyAccessEvent.Set();
            message.Print();
            break;

          case MsgType.Err:
            message.Print();
            await LeaveChat();
            return;

          case MsgType.Bye:
            Client.EndConnection();
            _chatInfo.CurrentState = State.End;
            return;

          default:
            await TransitionToErrorState();
            return;
        }
      }
    } catch (SocketException) {
      Logger.Log("Server disconnected");
      Client.EndConnection();
      _chatInfo.CurrentState = State.End;
    }
  }


  /// <summary>
  /// Read lines from the standard input until the message meant to be sent to the server is not
  /// read. Commands are processed internally. 
  /// </summary>
  /// <returns>The message to be sent to the server.</returns>
  private async Task<Message> GetUserInput() {
    while (true) {
      string? input = Console.ReadLine();

      // End of stdin
      if (input == null) {
        await LeaveChat();
      }

      // Empty line => skip
      if (input!.Length == 0) continue;

      // Text message
      if (!input.StartsWith('/')) {
        return new TextMessage(_chatInfo.DisplayName, Content: input, Id: Utils.Counter.GetNext());
      }

      // Message content too long
      if (input.Length > 1400) {
        Utils.PrintInternalError("Maximum message size is 1400 characters");
        continue;
      }

      // Command
      Message? message = _commandHandler.HandleCommand(input);

      if (message != null) {
        return message;
      }
    }
  }
}