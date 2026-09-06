using PingFlud.Application;
using Xunit;

namespace PingFlud.Application.Tests;

public sealed class SettingsDraftTests
{
    [Fact]
    public void InvalidDraftDoesNotMutateState()
    {
        var state = new AppState();
        var draft = SettingsDraft.From(state);
        draft.MaxOutstanding = 0;

        var applied = draft.TryApply(state, out var error);

        Assert.False(applied);
        Assert.NotEmpty(error);
        Assert.Equal(64, state.Settings.MaxOutstanding);
    }

    [Fact]
    public void ValidDraftAppliesSettingsAndBranding()
    {
        var state = new AppState();
        var draft = SettingsDraft.From(state);
        draft.TimeoutMs = 2500;
        draft.Title = "  Lab scanner  ";
        draft.Subtitle = " Authorized segment ";
        draft.ThemeName = "Daylight";

        var applied = draft.TryApply(state, out var error);

        Assert.True(applied, error);
        Assert.Equal(2500, state.Settings.TimeoutMs);
        Assert.Equal("Lab scanner", state.Title);
        Assert.Equal("Authorized segment", state.Subtitle);
        Assert.Equal("Daylight", state.ThemeName);
    }

    [Fact]
    public void RemovedThemeCannotBeApplied()
    {
        var state = new AppState();
        var draft = SettingsDraft.From(state);
        draft.ThemeName = "Nebula";

        var applied = draft.TryApply(state, out var error);

        Assert.False(applied);
        Assert.Contains("Unsupported theme", error);
        Assert.Equal("Graphite", state.ThemeName);
    }

    [Theory]
    [InlineData(true, "Graphite")]
    [InlineData(false, "Daylight")]
    public void AppearanceToggleMapsToSupportedTheme(bool isDarkMode, string expectedTheme)
    {
        var draft = new SettingsDraft { IsDarkMode = isDarkMode };

        Assert.Equal(expectedTheme, draft.ThemeName);
    }

    [Theory]
    [InlineData(1d, 1, 10, true, 1)]
    [InlineData(10d, 1, 10, true, 10)]
    [InlineData(1.5d, 1, 10, false, 0)]
    [InlineData(double.NaN, 1, 10, false, 0)]
    [InlineData(double.PositiveInfinity, 1, 10, false, 0)]
    [InlineData(0d, 1, 10, false, 0)]
    public void IntegerInputValidationRejectsNonFiniteFractionalAndOutOfRangeValues(
        double input,
        int minimum,
        int maximum,
        bool expected,
        int expectedValue)
    {
        var actual = SettingsDraft.TryConvertInteger(input, minimum, maximum, out var value);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedValue, value);
    }
}
