using ChatApp.Messages;

namespace ChatApp;

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

  public static void Log(string msg) {
    if (!_verbose) return;
    Console.Error.WriteLine($">>> {msg}");
  }

  public static void Log(string msg, Message message) {
    if (!_verbose) return;
    Console.Error.WriteLine($">>> [{message.MType}:{message.Id}] {msg}");
  }
}