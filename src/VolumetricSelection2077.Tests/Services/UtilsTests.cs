using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.Tests.Services;

public class UtilsTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, "0.000 seconds")]
    [InlineData(0, 0, 1, 0, "1.000 seconds")]
    [InlineData(0, 0, 1, 500, "1.500 seconds")]
    [InlineData(1, 0, 0, 0, "1 hour")]
    [InlineData(2, 0, 0, 0, "2 hours")]
    [InlineData(0, 1, 0, 0, "1 minute")]
    [InlineData(0, 2, 0, 0, "2 minutes")]
    [InlineData(1, 2, 3, 4, "1 hour, 2 minutes, 3.004 seconds")]
    public void FormatElapsedTime_FormatsLargestApplicableParts(int hours, int minutes, int seconds, int milliseconds,
        string expected)
    {
        var elapsed = new TimeSpan(0, hours, minutes, seconds, milliseconds);

        Assert.Equal(expected, Utils.FormatElapsedTime(elapsed));
    }

    [Theory]
    [InlineData(0, 0, "00:00")]
    [InlineData(0, 9, "00:09")]
    [InlineData(5, 9, "05:09")]
    [InlineData(59, 59, "59:59")]
    public void FormatElapsedTimeMMSS_FormatsMinutesAndSeconds(int minutes, int seconds, string expected)
    {
        var elapsed = new TimeSpan(0, minutes, seconds);

        Assert.Equal(expected, Utils.FormatElapsedTimeMMSS(elapsed));
    }

    [Theory]
    [InlineData("folder", "folder")]
    [InlineData(" folder ", "folder")]
    [InlineData(@"folder\bad*name?", @"folder\bad_name_")]
    [InlineData(@" C:\folder\ bad*name? \file.txt ", @"C:\folder\bad_name_\file.txt")]
    [InlineData(@"a\\b", @"a\b")]
    public void SanitizeFilePath_TrimsAndReplacesInvalidPathCharacters(string input, string expected)
    {
        Assert.Equal(expected, Utils.SanitizeFilePath(input));
    }

    [Fact]
    public void IsDirectoryEmpty_ReturnsTrueForEmptyDirectory()
    {
        var directory = CreateTempDirectory();
        try
        {
            Assert.True(Utils.IsDirectoryEmpty(directory));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void IsDirectoryEmpty_ReturnsFalseWhenDirectoryContainsFiles()
    {
        var directory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "file.txt"), "content");

            Assert.False(Utils.IsDirectoryEmpty(directory));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void IsDirectoryEmpty_ThrowsForMissingDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.Throws<ArgumentException>(() => Utils.IsDirectoryEmpty(directory));
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "vs2077-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
