using ChatApp.Enums;
using ChatApp.Messages;

namespace ChatApp.Tests;

public class UserCommandHandlerTests {
  private readonly object _stderrLock = new();
  private readonly StringWriter _errorOutput;
  private readonly StringWriter _outOutput;
  private readonly ChatData _chatInfo;
  private readonly UserCommandHandler _commandHandler;


  public UserCommandHandlerTests() {
    _chatInfo = new ChatData();
    _commandHandler = new UserCommandHandler(_chatInfo);
    _errorOutput = new StringWriter();
    _outOutput = new StringWriter();
    Console.SetError(TextWriter.Synchronized(_errorOutput));
    Console.SetOut(TextWriter.Synchronized(_outOutput));
  }

  [Theory]
  [InlineData(State.Start)]
  [InlineData(State.Auth)]
  [InlineData(State.Open)]
  [InlineData(State.Error)]
  public void HandleCommand_TC1_Help_ReturnsNullAndPrintsToStdOut(State state) {
    // Arrange
    _chatInfo.CurrentState = state;

    // Act
    var result = _commandHandler.HandleCommand("/help");

    // Assert
    Assert.Null(result);
    Assert.Empty(_errorOutput.ToString());
    Assert.NotEmpty(_outOutput.ToString());
  }

  [Theory]
  [InlineData(State.Start)]
  [InlineData(State.Auth)]
  public void HandleCommand_TC2_ValidAuth_CorrectState_ReturnsAuthMessage(State state) {
    // Arrange
    _chatInfo.CurrentState = state;

    // Act
    var result = _commandHandler.HandleCommand("/auth username password display name");

    // Assert
    Assert.IsType<AuthMessage>(result);
  }

  [Theory]
  [InlineData(State.Open)]
  [InlineData(State.Error)]
  public void HandleCommand_TC3_ValidAuth_IncorrectState_ReturnsAuthMessage(State state) {
    // Arrange
    _chatInfo.CurrentState = state;

    // Act
    var result = _commandHandler.HandleCommand("/auth username password display name");

    // Assert
    Assert.Null(result);
    Assert.StartsWith("ERR: ", _errorOutput.ToString());
  }

  [Fact]
  public void HandleCommand_TC4_InvalidAuth_ReturnsNull() {
    // Act
    var result = _commandHandler.HandleCommand("/auth INVALID INPUT");

    // Assert
    Assert.Null(result);
    Assert.StartsWith("ERR: ", _errorOutput.ToString());
  }


  [Theory]
  [InlineData(State.Open)]
  public void HandleCommand_TC4_ValidJoin_CorrectState_ReturnsJoinMessage(State state) {
    // Arrange
    _chatInfo.CurrentState = state;

    // Act
    var result = _commandHandler.HandleCommand("/join CHANNEL");

    // Assert
    Assert.IsType<JoinMessage>(result);
  }

  [Theory]
  [InlineData(State.Start)]
  [InlineData(State.Auth)]
  [InlineData(State.Error)]
  public void HandleCommand_TC6_ValidJoin_IncorrectState_ReturnsJoinMessage(State state) {
    lock (_stderrLock) {
      // Arrange
      _chatInfo.CurrentState = state;

      // Act
      var result = _commandHandler.HandleCommand("/join CHANNEL");

      // Assert
      Assert.Null(result);
      Assert.StartsWith("ERR: ", _errorOutput.ToString());
    }
  }

  [Fact]
  public void HandleCommand_TC7_InvalidJoin__ReturnsNull() {
    // Act
    var result = _commandHandler.HandleCommand("/join INVALID CHANNEL");

    // Assert
    Assert.Null(result);
    Assert.StartsWith("ERR: ", _errorOutput.ToString());
  }

  [Theory]
  [InlineData(State.Open)]
  public void HandleCommand_TC8_ValidRename_CorrectState_ReturnsNullAndChangeDisplayName(State state) {
    // Arrange
    _chatInfo.CurrentState = state;

    // Act
    var result = _commandHandler.HandleCommand("/rename TEST_NAME");

    // Assert
    Assert.Null(result);
    Assert.Equal("TEST_NAME", _chatInfo.DisplayName);
  }

  [Theory]
  [InlineData(State.Start)]
  [InlineData(State.Auth)]
  [InlineData(State.Error)]
  public void HandleCommand_TC9_ValidRename_IncorrectState_ReturnsNullAndPrintsError(State state) {
    // Arrange
    _chatInfo.CurrentState = state;

    // Act
    var result = _commandHandler.HandleCommand("/rename TEST_NAME");

    // Assert
    Assert.Null(result);
    Assert.StartsWith("ERR: ", _errorOutput.ToString());
  }

  [Fact]
  public void HandleCommand_TC10_InvalidRename__ReturnsNull() {
    // Act
    var result = _commandHandler.HandleCommand("/rename INVäLID NAME");

    // Assert
    Assert.Null(result);
    Assert.StartsWith("ERR: ", _errorOutput.ToString());
  }

  [Fact]
  public void HandleCommand_TC11_Unknown__ReturnsNullAndPrintsError() {
    // Act
    var result = _commandHandler.HandleCommand("/unknown");

    // Assert
    Assert.Null(result);
    Assert.StartsWith("ERR: ", _errorOutput.ToString());
  }
}