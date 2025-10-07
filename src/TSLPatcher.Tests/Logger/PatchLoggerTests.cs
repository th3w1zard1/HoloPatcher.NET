using TSLPatcher.Core.Logger;
using Xunit;
using FluentAssertions;

namespace TSLPatcher.Tests.Logger;

/// <summary>
/// Tests for PatchLogger functionality
/// </summary>
public class PatchLoggerTests
{
    [Fact]
    public void AddVerbose_ShouldAddLogWithVerboseType()
    {
        // Arrange
        var logger = new PatchLogger();

        // Act
        logger.AddVerbose("Test verbose message");

        // Assert
        logger.AllLogs.Should().HaveCount(1);
        logger.AllLogs[0].LogType.Should().Be(LogType.Verbose);
        logger.AllLogs[0].Message.Should().Be("Test verbose message");
    }

    [Fact]
    public void AddNote_ShouldAddLogWithNoteType()
    {
        // Arrange
        var logger = new PatchLogger();

        // Act
        logger.AddNote("Test note message");

        // Assert
        logger.AllLogs.Should().HaveCount(1);
        logger.AllLogs[0].LogType.Should().Be(LogType.Note);
        logger.AllLogs[0].Message.Should().Be("Test note message");
    }

    [Fact]
    public void AddWarning_ShouldAddLogWithWarningType()
    {
        // Arrange
        var logger = new PatchLogger();

        // Act
        logger.AddWarning("Test warning message");

        // Assert
        logger.AllLogs.Should().HaveCount(1);
        logger.AllLogs[0].LogType.Should().Be(LogType.Warning);
        logger.AllLogs[0].Message.Should().Be("Test warning message");
    }

    [Fact]
    public void AddError_ShouldAddLogWithErrorType()
    {
        // Arrange
        var logger = new PatchLogger();

        // Act
        logger.AddError("Test error message");

        // Assert
        logger.AllLogs.Should().HaveCount(1);
        logger.AllLogs[0].LogType.Should().Be(LogType.Error);
        logger.AllLogs[0].Message.Should().Be("Test error message");
    }

    [Fact]
    public void LogAdded_EventShouldFireWhenLogAdded()
    {
        // Arrange
        var logger = new PatchLogger();
        PatchLog? addedLog = null;
        logger.LogAdded += (sender, log) => addedLog = log;

        // Act
        logger.AddNote("Test message");

        // Assert
        addedLog.Should().NotBeNull();
        addedLog!.Message.Should().Be("Test message");
        addedLog.LogType.Should().Be(LogType.Note);
    }

    [Fact]
    public void VerboseLogs_ShouldFilterByVerboseType()
    {
        // Arrange
        var logger = new PatchLogger();
        logger.AddVerbose("Verbose 1");
        logger.AddNote("Note 1");
        logger.AddVerbose("Verbose 2");
        logger.AddWarning("Warning 1");

        // Act
        var verboseLogs = logger.VerboseLogs.ToList();

        // Assert
        verboseLogs.Should().HaveCount(2);
        verboseLogs.All(l => l.LogType == LogType.Verbose).Should().BeTrue();
    }

    [Fact]
    public void Notes_ShouldFilterByNoteType()
    {
        // Arrange
        var logger = new PatchLogger();
        logger.AddNote("Note 1");
        logger.AddVerbose("Verbose 1");
        logger.AddNote("Note 2");

        // Act
        var notes = logger.Notes.ToList();

        // Assert
        notes.Should().HaveCount(2);
        notes.All(l => l.LogType == LogType.Note).Should().BeTrue();
    }

    [Fact]
    public void Warnings_ShouldFilterByWarningType()
    {
        // Arrange
        var logger = new PatchLogger();
        logger.AddWarning("Warning 1");
        logger.AddNote("Note 1");
        logger.AddWarning("Warning 2");

        // Act
        var warnings = logger.Warnings.ToList();

        // Assert
        warnings.Should().HaveCount(2);
        warnings.All(l => l.LogType == LogType.Warning).Should().BeTrue();
    }

    [Fact]
    public void Errors_ShouldFilterByErrorType()
    {
        // Arrange
        var logger = new PatchLogger();
        logger.AddError("Error 1");
        logger.AddNote("Note 1");
        logger.AddError("Error 2");

        // Act
        var errors = logger.Errors.ToList();

        // Assert
        errors.Should().HaveCount(2);
        errors.All(l => l.LogType == LogType.Error).Should().BeTrue();
    }

    [Fact]
    public void MultipleLogTypes_ShouldMaintainOrder()
    {
        // Arrange
        var logger = new PatchLogger();

        // Act
        logger.AddVerbose("Message 1");
        logger.AddNote("Message 2");
        logger.AddWarning("Message 3");
        logger.AddError("Message 4");

        // Assert
        logger.AllLogs.Should().HaveCount(4);
        logger.AllLogs[0].LogType.Should().Be(LogType.Verbose);
        logger.AllLogs[1].LogType.Should().Be(LogType.Note);
        logger.AllLogs[2].LogType.Should().Be(LogType.Warning);
        logger.AllLogs[3].LogType.Should().Be(LogType.Error);
    }

    [Fact]
    public void ThreadSafety_ShouldHandleConcurrentAccess()
    {
        // Arrange
        var logger = new PatchLogger();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() => logger.AddNote($"Message {index}")));
        }
        Task.WaitAll(tasks.ToArray());

        // Assert
        logger.AllLogs.Should().HaveCount(100);
    }
}

