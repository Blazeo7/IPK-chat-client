using System.Diagnostics;
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

    // handle interruption signal Ctrl^C
    Console.CancelKeyPress += async (sender, e) => await LeaveChat();

    while (CurrentState != State.End) {
      await (CurrentState switch {
        State.Start => StartState(),
        State.Auth => AuthState(),
        State.Open => OpenState(),
        State.Error => ErrorState(),
        _ => throw new UnreachableException("Invalid state"),
      });
    }
  }


  /// <summary>
  /// Allows user to send the authentication message. If the message was sent the state will be
  /// changed to <see cref="State.Auth"/>. 
  /// </summary>
  private async Task StartState() {
    var startMsg = GetUserInput();
    switch (startMsg.MType) {
      case MsgType.Auth:
        await Client.SendMessage(startMsg);
        CurrentState = State.Auth;
        break;

      default:
        new ErrorMessage(content: "You need to authenticate using valid /auth command").Print();
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
        CurrentState = State.End;
        break;

      default:
        await Client.SendMessage(new ErrorMessage(displayName: "Server",
          content: "Invalid response from server"));
        CurrentState = State.Error;
        break;
    }
  }

  /// <summary>
  /// Allows user to send and receive messages from server at the same time. Move to next state
  /// is only possible when <see cref="ReceiveInOpenState"/> task ends.
  /// </summary>
  private async Task OpenState() {
    var receiving = Task.Run(ReceiveInOpenState);
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
    await Client.SendMessage(new ByeMessage());
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
          await Client.SendMessage(message);
          break;

        case MsgType.Auth:
          new ErrorMessage("You are already logged in").Print();
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
  private async Task ReceiveInOpenState() {
    while (true) {
      var openMessage = await Client.ReceiveMessage();
      switch (openMessage.MType) {
        case MsgType.Msg:
          openMessage.Print();
          break;

        case MsgType.Err:
          await LeaveChat();
          CurrentState = State.End;
          return;

        case MsgType.Bye:
          CurrentState = State.End;
          return;

        default:
          await Client.SendMessage(new ErrorMessage(displayName: "Server",
            content: "Invalid Message from server"));
          CurrentState = State.Error;
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
    try {
      while (true) {
        string? input = Console.ReadLine()?.Trim();

        // Interruption signal => do nothing (handled in another thread)
        if (input == null) {
          continue;
        }

        // Text message
        if (!input.StartsWith('/')) {
          return new TextMessage(DisplayName, input);
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
            Console.WriteLine($"Unknown command `{input}`");
            PrintHelpMessage();
            break;
        }
      }
    } catch (IOException e) {
      Console.WriteLine(e);
      throw;
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

    return new AuthMessage(username, password, DisplayName, Utils.Counter.GetNextValue());
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
      return new ErrorMessage("Invalid /join command syntax");
    }

    var channelId = joinMatch.Groups[1].Value;
    return new JoinMessage(channelId, DisplayName, Utils.Counter.GetNextValue());
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
      Console.WriteLine("Invalid syntax for /rename command");
      PrintHelpMessage();
      return;
    }

    switch (CurrentState) {
      case State.Start:
      case State.Auth:
        Console.WriteLine("Error: You are not logged in, please use /auth command");
        break;

      default:
        DisplayName = renameMatch.Groups[1].Value;
        Console.WriteLine($"Name changed to `{DisplayName}`");
        break;
    }
  }

  /// <summary>
  /// Prints help message that shows the valid chat commands.
  /// </summary>
  private static void PrintHelpMessage() {
    Console.WriteLine(
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