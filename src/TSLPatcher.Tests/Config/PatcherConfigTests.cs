using TSLPatcher.Core.Config;
using Xunit;
using FluentAssertions;

namespace TSLPatcher.Tests.Config;

/// <summary>
/// Tests for PatcherConfig functionality
/// Ported from tests/tslpatcher/test_config.py
/// </summary>
public class PatcherConfigTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var config = new PatcherConfig();

        // Assert
        config.WindowTitle.Should().BeEmpty();
        config.ConfirmMessage.Should().BeEmpty();
        config.GameNumber.Should().BeNull();
        config.LogLevel.Should().Be(LogLevel.Warnings);
        config.RequiredFiles.Should().BeEmpty();
        config.RequiredMessages.Should().BeEmpty();
        config.InstallList.Should().BeEmpty();
        config.Patches2DA.Should().BeEmpty();
    }

    [Fact]
    public void WindowTitle_ShouldBeSettable()
    {
        // Arrange
        var config = new PatcherConfig();

        // Act
        config.WindowTitle = "Test Mod Installer";

        // Assert
        config.WindowTitle.Should().Be("Test Mod Installer");
    }

    [Fact]
    public void ConfirmMessage_ShouldBeSettable()
    {
        // Arrange
        var config = new PatcherConfig();

        // Act
        config.ConfirmMessage = "Install this mod?";

        // Assert
        config.ConfirmMessage.Should().Be("Install this mod?");
    }

    [Fact]
    public void GameNumber_ShouldBeSettable()
    {
        // Arrange
        var config = new PatcherConfig();

        // Act
        config.GameNumber = 1;

        // Assert
        config.GameNumber.Should().Be(1);
    }

    [Fact]
    public void LogLevel_ShouldBeSettable()
    {
        // Arrange
        var config = new PatcherConfig();

        // Act
        config.LogLevel = LogLevel.Full;

        // Assert
        config.LogLevel.Should().Be(LogLevel.Full);
    }

    [Fact]
    public void RequiredFiles_ShouldBeModifiable()
    {
        // Arrange
        var config = new PatcherConfig();

        // Act
        config.RequiredFiles.Add(new[] { "dialog.tlk", "chitin.key" });
        config.RequiredFiles.Add(new[] { "swkotor2.exe" });

        // Assert
        config.RequiredFiles.Should().HaveCount(2);
        config.RequiredFiles[0].Should().BeEquivalentTo(new[] { "dialog.tlk", "chitin.key" });
        config.RequiredFiles[1].Should().BeEquivalentTo(new[] { "swkotor2.exe" });
    }

    [Fact]
    public void RequiredMessages_ShouldBeModifiable()
    {
        // Arrange
        var config = new PatcherConfig();

        // Act
        config.RequiredMessages.Add("This file is required!");
        config.RequiredMessages.Add("Another required file");

        // Assert
        config.RequiredMessages.Should().HaveCount(2);
        config.RequiredMessages[0].Should().Be("This file is required!");
    }

    [Fact]
    public void SaveProcessedScripts_ShouldBeSettable()
    {
        // Arrange
        var config = new PatcherConfig();

        // Act
        config.SaveProcessedScripts = 1;

        // Assert
        config.SaveProcessedScripts.Should().Be(1);
    }

    [Fact]
    public void IgnoreFileExtensions_ShouldBeSettable()
    {
        // Arrange
        var config = new PatcherConfig();

        // Act
        config.IgnoreFileExtensions = true;

        // Assert
        config.IgnoreFileExtensions.Should().BeTrue();
    }
}

