using ClientServer.Messages;

namespace ClientServer;

/// <summary>
/// Simple logger class for debugging purposes.
/// </summary>
public class Logger {
  private static bool _verbose;

  /// <summary>
  /// Enable logging based on value of <see cref="verbose"/>.
  /// </summary>
  /// <param name="verbose">True for verbose logging, false otherwise.</param>
  public static void Init(bool verbose) {
    _verbose = verbose;
  }

  /// <summary>
  /// Logs a message to the console if verbosity is enabled.
  /// </summary>
  /// <param name="msg">The message to be logged.</param>
  /// <param name="message">Prints its type and id</param>
  public static void Log(string msg, Message? message = null) {
    if (!_verbose) return;

    Console.Error.WriteLine(message == null
      ? $">>> {msg}"
      : $">>> [{message.MType}:{message.MsgId}] {msg}");
  }
}