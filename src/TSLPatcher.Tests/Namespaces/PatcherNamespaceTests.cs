using TSLPatcher.Core.Namespaces;
using Xunit;
using FluentAssertions;

namespace TSLPatcher.Tests.Namespaces;

/// <summary>
/// Tests for PatcherNamespace functionality
/// </summary>
public class PatcherNamespaceTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var ns = new PatcherNamespace();

        // Assert
        ns.NamespaceId.Should().BeEmpty();
        ns.IniFilename.Should().Be(PatcherNamespace.DefaultIniFilename);
        ns.InfoFilename.Should().Be(PatcherNamespace.DefaultInfoFilename);
        ns.DataFolderPath.Should().BeEmpty();
        ns.Name.Should().BeEmpty();
        ns.Description.Should().BeEmpty();
    }

    [Fact]
    public void NamespaceId_ShouldBeSettable()
    {
        // Arrange
        var ns = new PatcherNamespace();

        // Act
        ns.NamespaceId = "mod1";

        // Assert
        ns.NamespaceId.Should().Be("mod1");
    }

    [Fact]
    public void IniFilename_ShouldBeSettable()
    {
        // Arrange
        var ns = new PatcherNamespace();

        // Act
        ns.IniFilename = "custom.ini";

        // Assert
        ns.IniFilename.Should().Be("custom.ini");
    }

    [Fact]
    public void InfoFilename_ShouldBeSettable()
    {
        // Arrange
        var ns = new PatcherNamespace();

        // Act
        ns.InfoFilename = "readme.rtf";

        // Assert
        ns.InfoFilename.Should().Be("readme.rtf");
    }

    [Fact]
    public void DataFolderPath_ShouldBeSettable()
    {
        // Arrange
        var ns = new PatcherNamespace();

        // Act
        ns.DataFolderPath = "C:\\mods\\test";

        // Assert
        ns.DataFolderPath.Should().Be("C:\\mods\\test");
    }

    [Fact]
    public void ChangesFilePath_ShouldCombineFolderAndIniFilename()
    {
        // Arrange
        var ns = new PatcherNamespace
        {
            DataFolderPath = "C:\\mods\\test",
            IniFilename = "changes.ini"
        };

        // Act
        var path = ns.ChangesFilePath();

        // Assert
        path.Should().Be("C:\\mods\\test\\changes.ini");
    }

    [Fact]
    public void RtfFilePath_ShouldCombineFolderAndInfoFilename()
    {
        // Arrange
        var ns = new PatcherNamespace
        {
            DataFolderPath = "C:\\mods\\test",
            InfoFilename = "info.rtf"
        };

        // Act
        var path = ns.RtfFilePath();

        // Assert
        path.Should().Be("C:\\mods\\test\\info.rtf");
    }

    [Fact]
    public void Name_ShouldBeSettable()
    {
        // Arrange
        var ns = new PatcherNamespace();

        // Act
        ns.Name = "Test Mod";

        // Assert
        ns.Name.Should().Be("Test Mod");
    }

    [Fact]
    public void Description_ShouldBeSettable()
    {
        // Arrange
        var ns = new PatcherNamespace();

        // Act
        ns.Description = "This is a test mod";

        // Assert
        ns.Description.Should().Be("This is a test mod");
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var ns = new PatcherNamespace
        {
            NamespaceId = "mod1",
            Name = "Test Mod"
        };

        // Act
        var result = ns.ToString();

        // Assert
        result.Should().Contain("mod1");
        result.Should().Contain("Test Mod");
    }
}

