using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClientServer;

public class Utils {
  public static byte[] AsBytes(byte type, short id, params string[] parameters) {
    using MemoryStream stream = new();
    using BinaryWriter writer = new(stream);

    writer.Write(type);
    writer.Write(id);

    // Write values to the stream
    foreach (var param in parameters) {
      byte[] stringBytes = Encoding.UTF8.GetBytes(param);
      writer.Write(stringBytes);
      writer.Write((byte)0);
    }

    // Get the resulting byte array
    return stream.ToArray();
  }


  // Find strings based on \0 separator in `data`
  public static string[] FromBytes(byte[] data, int index = 0) {
    string str = Encoding.UTF8.GetString(data, index, data.Length - index);
    return str.Split("\0", StringSplitOptions.RemoveEmptyEntries);
  }

  public static IPAddress ConvertHostname2IPv4(string hostname) {
    IPAddress[] addresses = Dns.GetHostAddresses(hostname);

    foreach (IPAddress address in addresses) {
      if (address.AddressFamily == AddressFamily.InterNetwork) {
        return address;
      }
    }

    throw new SocketException();
  }
  
  internal static class Counter {
    private static short _count;

    public static short GetNextValue() {
      return ++_count;
    }
  }
}