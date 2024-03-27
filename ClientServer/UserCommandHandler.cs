using ClientServer.Enums;
using ClientServer.Messages;
using System.Text.RegularExpressions;

namespace ClientServer;

public class UserCommandHandler(ChatData chatInfo) {
  /// <summary>
  /// Handles user input and returns a <see cref="Message"/> based on the provided input. The
  /// local commands (/help or /rename) are handled internally. The function is called until
  /// user typed /auth or /join command.
  /// </summary>
  /// <returns>
  /// <list type="bullet">
  ///     <item><see cref="AuthMessage"/>: /auth command </item>
  ///     <item><see cref="JoinMessage"/>: /join command</item>
  /// </list>
  /// </returns>
  public Message? HandleCommand(string input) {
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
        PrintError($"Unknown command `{input}`", showHelp: true);
        break;
    }

    return null;
  }


  /// <summary>
  /// Handle /auth command.
  /// </summary>
  /// <param name="input">The text that contains /auth command.</param>
  /// <returns>
  /// <list type="bullet">
  ///     <item><description><see cref="ErrorMessage"/>: In case of invalid arguments.</description></item>
  ///     <item><description><see cref="AuthMessage"/>: If the auth command is valid and the display name is set based on the
  /// corresponding argument.</description></item>
  /// </list>
  /// </returns>
  private Message? HandleAuthCommand(string input) {
    // > /auth {username} {secret} {display name}
    var authMatch = Regex.Match(input,
      @"^\/auth\s+([a-zA-Z0-9-]{1,20})\s+([a-zA-Z0-9-]{1,128})\s+([ -~]{1,20})\s*$");

    // invalid auth instruction format => print help message
    if (!authMatch.Success) {
      PrintError("Invalid format of /auth command", showHelp: true);
      return null;
    }

    if (chatInfo.CurrentState is State.Open or State.Error) {
      PrintError("You are already authenticate");
      return null;
    }

    var username = authMatch.Groups[1].Value;
    var password = authMatch.Groups[2].Value;
    chatInfo.DisplayName = authMatch.Groups[3].Value;

    return new AuthMessage(Username: username, Secret: password, DisplayName: chatInfo.DisplayName,
      Id: Utils.Counter.GetNext());
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
  private Message? HandleJoinCommand(string input) {
    // > /join {channel id}
    var joinMatch = Regex.Match(input, @"^\/join\s+([a-zA-Z0-9-]{1,20})\s*$");

    if (!joinMatch.Success) {
      PrintError("Invalid /join command syntax", showHelp: true);
      return null;
    }

    if (chatInfo.CurrentState is State.Start or State.Auth or State.Error) {
      PrintError("You cannot change channel in the current state");
      return null;
    }

    string channelId = joinMatch.Groups[1].Value;
    return new JoinMessage(channelId, chatInfo.DisplayName, Utils.Counter.GetNext());
  }

  /// <summary>
  /// Check validity of /rename command and in case of success changes user display name.
  /// </summary>
  /// <param name="input">The text that contains /rename command.</param>
  private void HandleRenameCommand(string input) {
    // > /rename {display name}
    var renameMatch = Regex.Match(input, @"^\/rename\s+([!-~]{1,20})\s*$");

    if (!renameMatch.Success) {
      PrintError("Invalid syntax for /rename command", showHelp: true);
      return;
    }

    if (chatInfo.CurrentState is State.Start or State.Auth or State.Error) {
      PrintError("You are not logged in, please use /auth command");
      return;
    }

    chatInfo.DisplayName = renameMatch.Groups[1].Value;
    Logger.Log($"Name changed to `{chatInfo.DisplayName}`");
  }

  /// <summary>
  /// Prints help message that shows the valid chat commands.
  /// </summary>
  private static void PrintHelpMessage() {
    Console.Out.WriteLine(
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

  private static void PrintError(string message, bool showHelp = false) {
    Utils.PrintInternalError(message);

    if (showHelp) {
      PrintHelpMessage();
    }
  }
}