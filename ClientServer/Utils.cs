using System.Net;
using System.Net.Sockets;
using System.Text;
using ClientServer.Enums;

namespace ClientServer;

public class Utils {
  /// <summary>
  /// Create array of bytes from its parameters
  /// </summary>
  /// <returns>Byte array representing arguments values one after another</returns>
  public static byte[] AsBytes(params dynamic[] parameters) {
    using MemoryStream stream = new();
    using BinaryWriter writer = new(stream);

    // Write values to the stream
    foreach (var param in parameters) {
      if (param is string) {
      byte[] stringBytes = Encoding.UTF8.GetBytes(param);
      writer.Write(stringBytes);
      writer.Write((byte)0);
      } else {
        writer.Write(param);
    }
    }

    // Get the resulting byte array
    return stream.ToArray();
  }


  /// <summary>
  /// Get array of strings from bytes.
  /// </summary>
  /// <param name="data">byte array that contains strings (separated by \0)</param>
  /// <param name="index">Start of the first string</param>
  /// <returns>Returns all strings from byte array based on \0 separator in <see cref="data"/></returns>
  public static string[] FromBytes(byte[] data, int index = 0) {
    string str = Encoding.UTF8.GetString(data, index, data.Length - index);
    return str.Split("\0", StringSplitOptions.RemoveEmptyEntries);
  }

  /// <summary>
  /// Convert <see cref="hostname"/> to IPv4 address
  /// </summary>
  /// <param name="hostname"></param>
  /// <returns>IP address corresponding to the <see cref="hostname"/></returns>
  /// <exception cref="SocketException"></exception>
  public static IPAddress ConvertHostname2IPv4(string hostname) {
    IPAddress[] addresses = Dns.GetHostAddresses(hostname);

    foreach (IPAddress address in addresses) {
      if (address.AddressFamily == AddressFamily.InterNetwork) {
        return address;
      }
    }

    throw new SocketException();
  }

  public static class Counter {
    private static ushort _count;

    public static ushort GetNext() {
      return ++_count;
    }
  }

  public static ReplyResult ReplyResultFromInt(byte a) {
    return a switch {
      0 => ReplyResult.Nok,
      1 => ReplyResult.Ok,
      _ => ReplyResult.Invalid,
    };
  }

  public static void PrintInternalError(string message) {
    Console.Error.WriteLine($"ERR: {message}");
  }

  public static ushort HostToNetworkOrder(ushort value) {
    // Check if the system is little-endian
    bool isLittleEndian = BitConverter.IsLittleEndian;

    if (isLittleEndian) {
      return (ushort)((value >> 8) | (value << 8));
    } else {
      return value;
    }
  }
}