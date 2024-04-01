using ChatApp.Enums;
using ChatApp.Messages;
using ChatApp.Tests.Servers;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ChatApp.Tests;

public class ChatSimulator(Server server) {
  public List<MsgType> ExchangedMessages = [];

  private Process _process = null!;

  public async Task Simulate(params Func<Task>[] callbacks) {
    Logger.Init(true);
    _process = new Process();
    ExchangedMessages = [];

    Task clientTask = RunProcessAsync();
    Task serverTask = RunServer(callbacks);
    Task timeout = Task.Delay(millisecondsDelay: 2500);

    // Wait for both tasks to complete
    Task communicationTask = Task.WhenAll(clientTask, serverTask);
    await Task.WhenAny(timeout, communicationTask);

    if (timeout.IsCompleted) {
      Logger.Log("Test Timeout");
      if (!clientTask.IsCompleted) {
        _process.Kill();
      }
    }
  }

  public async Task SendMessage(Message message) {
    Logger.Log("Server sending", message);
    ExchangedMessages.Add(message.MType);
    await server.SendMessage(message);
  }

  public async Task ReceiveMessage(string? input) {
    if (input == null) {
      // Close the standard input to signal the end of input
      _process.StandardInput.Close();
    } else {
      await _process.StandardInput.WriteLineAsync(input);
    }

    Message message = await server.ReceiveMessage();
    ExchangedMessages.Add(message.MType);
    Logger.Log("Server received", message);
  }

  private async Task RunServer(params Func<Task>[] callbacks) {
    server.SetUpConnection();
    await server.AcceptClient();

    Logger.Log("Connection set up");
    try {
      foreach (var callback in callbacks) {
        await callback();
      }
    } catch (SocketException) {
      Logger.Log("Socket exception");
    }

    Logger.Log("Server Disconnected");
    server.EndConnection();
  }


  private async Task RunProcessAsync() {
    string programPath;

    // Check if the operating system is Windows
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
      programPath = "ChatApp.exe";
    }
    // Check if the operating system is Linux
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
      programPath = "ChatApp";
    } else {
      throw new PlatformNotSupportedException("This operating system is not supported.");
    }

    _process.StartInfo.FileName = programPath;
    _process.StartInfo.RedirectStandardInput = true;

    if (server is UdpServer server1) {
      _process.StartInfo.Arguments =
        $"-t udp -s {server1.Hostname} -p {server1.Port} -v -r {server1.Retries} -d {server1.Timeout}";
    } else {
      _process.StartInfo.Arguments =
        $"-t tcp -s {server.Hostname} -p {server.Port} -v";
    }

    _process.Start();

    // Wait for the _process to exit
    await _process.WaitForExitAsync();
    _process.Dispose();
  }
}