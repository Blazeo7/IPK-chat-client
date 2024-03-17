using System.Diagnostics;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using ClientServer.Clients;
using ClientServer.Enums;
using ClientServer.Messages;

namespace ClientServer;

/// <summary>
/// Represents a class responsible for handling communication with server.
/// </summary>
public class ChatClient(BaseClient client) {
  protected State CurrentState { get; set; } = State.Start;
  protected BaseClient Client { get; init; } = client;
  protected string DisplayName { get; set; } = "default";

  /// <summary>
  /// Leads communication from set up phase until the disconnection from server
  /// </summary>
  public async Task Communicate() {
    Client.SetUpConnection();
    _chatInfo.Connected = true;
    // handle interruption signal Ctrl^C
    Console.CancelKeyPress += async (_, e) => {
      // Prevent the default behavior (program termination)
      e.Cancel = true;

      // Execute the LeaveChat function
      await LeaveChat();

      Environment.Exit(0);
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
    var message = GetUserInput();
    switch (message.MType) {
      case MsgType.Auth:
        bool isSuccess = await Client.SendMessage(message);
        if (isSuccess) {
          _chatInfo.CurrentState = State.Auth;
        } else {
          Logger.Log("Message not confirmed");
          await LeaveChat();
          Environment.Exit(1);
        }

        break;

      default:
        Utils.PrintInternalError("You need to authenticate using valid /auth command");
        break;
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
        CurrentState = ((ReplyMessage)receiveMessage).Result == 1 ? State.Open : State.Start;
        receiveMessage.Print();
        break;

      case MsgType.Err:
        await LeaveChat();
        receiveMessage.Print();
        CurrentState = State.End;
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
    var receiving = Task.Run(OpenStateReceiving);
    var sending = Task.Run(SendInOpenState);
    await Task.WhenAny(receiving, sending);
  }


  /// <summary>
  /// Leave chat and change state to <see cref="State.End"/>. 
  /// </summary>
  private async Task ErrorState() {
    await LeaveChat();
    CurrentState = State.End;
  }

  /// <summary>
  /// Gracefully leaves chat and end connection with server
  /// </summary>
  private async Task LeaveChat() {
    Logger.Log("Leaving");
    await Client.SendMessage(new ByeMessage(id: Utils.Counter.GetNext()));
    Client.EndConnection();
  }

  /// <summary>
  /// Allow sending messages to server nonstop until it is not killed by other the thread.
  /// </summary>
  private async Task SendInOpenState() {
    while (true) {
      Message message = GetUserInput();

      switch (message.MType) {
        case MsgType.Msg:
        case MsgType.Join:
          bool isSuccess = await Client.SendMessage(message);

          if (!isSuccess) { // message was not received by the server
            await LeaveChat();
            CurrentState = State.End;
          }

          break;

        case MsgType.Auth:
          Utils.PrintInternalError("You are already logged in");
          break;

        case MsgType.Err:
          message.Print();
          break;

        default:
          throw new UnreachableException();
      }
    }
  }

  /// <summary>
  /// Allows user asynchronously receiving text messages from the server until. Server subscribing
  /// is interrupted when received message is not TextMessage.
  /// </summary>
  private async Task OpenStateReceiving() {
    while (true) {
      var openMessage = await Client.ReceiveMessage();
      switch (openMessage.MType) {
        case MsgType.Reply:
        case MsgType.Msg:
          openMessage.Print();
          break;

        case MsgType.Err:
          openMessage.Print();
          await LeaveChat();
          CurrentState = State.End;
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
  }


  /// <summary>
  /// Handles user input and returns a <see cref="Message"/> based on the provided input. The
  /// local commands (/help or /rename) are handled internally. The function is called until
  /// user typed /auth or /join command.
  /// </summary>
  /// <returns>
  /// <list type="bullet">
  ///     <item><see cref="AuthMessage"/>: /auth command </item>
  ///     <item><see cref="JoinMessage"/>: /join command</item>
  ///     <item><see cref="ErrorMessage"/>: invalid command (starts with `/`)</item>
  ///     <item><see cref="TextMessage"/>: if user did not write any command</item>
  /// </list>
  /// </returns>
  private Message GetUserInput() {
    while (true) {
      string? input = Console.ReadLine()?.Trim();

      // Interruption signal => do nothing (handled in another thread)
      if (input == null) {
        continue;
      }

      // Text message
      if (!input.StartsWith('/')) {
        return new TextMessage(_chatInfo.DisplayName, Content: input, Id: Utils.Counter.GetNext());
      }

      // Command
      switch (input) {
        case var _ when input.StartsWith(UserCommands.Help):
          PrintHelpMessage();
          break;

        case var _ when input.StartsWith(UserCommands.Auth):
          return HandleAuthCommand(input);

        case var _ when input.StartsWith(UserCommands.Join):
          return HandleJoinCommand(input);

        case var _ when input.StartsWith(UserCommands.Rename):
          HandleRenameCommand(input);
          break;

        default: // unknown command
          Console.Error.WriteLine($"Unknown command `{input}`");
          PrintHelpMessage();
          break;
      }
    }
  }


  /// <summary>
  /// Handle /auth command.
  /// </summary>
  /// <param name="input">The text that contains /auth command.</param>
  /// <returns>
  /// <list type="bullet">
  ///     <item><description><see cref="ErrorMessage"/>: In case of invalid arguments.</description></item>
  ///     <item><description><see cref="AuthMessage"/>: If the auth command is valid and the <see cref="DisplayName"/> is set based on the
  /// corresponding argument.</description></item>
  /// </list>
  /// </returns>
  private Message HandleAuthCommand(string input) {
    // > /auth {username} {secret} {display name}
    const string authPattern =
      @"^\/auth\s+([a-zA-Z0-9-]{1,20})\s+([a-zA-Z0-9-]{1,128})\s+([ -~]{1,20})\s*$";
    var authMatch = Regex.Match(input, authPattern);

    // invalid auth instruction format => print help message
    if (!authMatch.Success) {
      return new ErrorMessage("Invalid format of /auth command");
    }

    var username = authMatch.Groups[1].Value;
    var password = authMatch.Groups[2].Value;
    DisplayName = authMatch.Groups[3].Value;

    return new AuthMessage(username, password, DisplayName, Utils.Counter.GetNext());
  }

  /// <summary>
  /// Handle /join command.
  /// </summary>
  /// <param name="input">The text that contains /join command.</param>
  /// <returns>
  /// <list type="bullet">
  ///     <item><description><see cref="ErrorMessage"/>: In case of invalid arguments.</description></item>
  ///     <item><description><see cref="JoinMessage"/>: If the join command is valid.</description></item>
  /// </list>
  /// </returns>
  private Message HandleJoinCommand(string input) {
    // > /join {channel id}
    const string joinPattern = @"^\/join\s+([a-zA-Z0-9-]{1,20})\s*$";
    var joinMatch = Regex.Match(input, joinPattern);

    if (!joinMatch.Success) {
      return new ErrorMessage(content: "Invalid /join command syntax");
    }

    var channelId = joinMatch.Groups[1].Value;
    return new JoinMessage(channelId, DisplayName, Utils.Counter.GetNext());
  }

  /// <summary>
  /// Check validity of /rename command and in case of success changes user <see cref="DisplayName"/>.
  /// </summary>
  /// <param name="input">The text that contains /rename command.</param>
  private void HandleRenameCommand(string input) {
    // > /rename {display name}
    const string renamePattern = @"^\/rename\s+([!-~]{1,20})\s*$";
    var renameMatch = Regex.Match(input, renamePattern);

    if (!renameMatch.Success) {
      new ErrorMessage("Invalid syntax for /rename command").Print();
      PrintHelpMessage();
      return;
    }

    switch (CurrentState) {
      case State.Start:
      case State.Auth:
        new ErrorMessage("You are not logged in, please use /auth command").Print();
        break;

      default:
        DisplayName = renameMatch.Groups[1].Value;
        Logger.Log($"Name changed to `{DisplayName}`");
        break;
    }
  }

  /// <summary>
  /// Prints help message that shows the valid chat commands.
  /// </summary>
  private static void PrintHelpMessage() {
    Console.Error.WriteLine(
      """
      Command    Parameters                        Using
      /auth      {Username} {Secret} {DisplayName} Sends AUTH message with the data provided from
                                                   the command to the server, locally sets the DisplayName
      /join      {ChannelID}                       Sends JOIN message with channel name from
                                                   the command to the server
      /rename    {DisplayName}                     Locally changes the display name of the user
      /help                                        Prints this help usage
      """);
  }
}