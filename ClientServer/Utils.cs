using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

}