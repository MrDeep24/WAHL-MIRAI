using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests;

public class ActiveSessionStoreTests
{
    [Fact]
    public void RegisterAndValidate_HappyPath_ReturnsValid()
    {
        // Arrange
        var store = new ActiveSessionStore();
        uint userId = 10;
        string stamp = "session-guid-1";

        // Act
        store.RegisterSession(userId, stamp);
        var (isValid, reason) = store.IsSessionValid(userId, stamp);

        // Assert
        Assert.True(isValid);
        Assert.Null(reason);
    }

    [Fact]
    public void SecondRegisterSession_InvalidatesFirstStampWithReplacedByNewDevice()
    {
        // Arrange
        var store = new ActiveSessionStore();
        uint userId = 10;
        string stamp1 = "device-1-stamp";
        string stamp2 = "device-2-stamp";

        // Act
        store.RegisterSession(userId, stamp1);
        store.RegisterSession(userId, stamp2);

        var (oldStampValid, oldStampReason) = store.IsSessionValid(userId, stamp1);
        var (newStampValid, newStampReason) = store.IsSessionValid(userId, stamp2);

        // Assert
        Assert.False(oldStampValid);
        Assert.Equal(SessionInvalidationReason.ReplacedByNewDevice, oldStampReason);

        Assert.True(newStampValid);
        Assert.Null(newStampReason);
    }

    [Fact]
    public void ExplicitInvalidateSession_SoftDeleteOrDisable_ReturnsFalseWithAccountDisabledOrDeleted()
    {
        // Arrange
        var store = new ActiveSessionStore();
        uint userId = 25;
        string stamp = "active-session-stamp";

        // Act
        store.RegisterSession(userId, stamp);
        store.InvalidateSession(userId, SessionInvalidationReason.AccountDisabledOrDeleted);
        var (isValid, reason) = store.IsSessionValid(userId, stamp);

        // Assert
        Assert.False(isValid);
        Assert.Equal(SessionInvalidationReason.AccountDisabledOrDeleted, reason);
    }

    [Fact]
    public void ExplicitInvalidateSession_Logout_ReturnsFalseWithLoggedOut()
    {
        // Arrange
        var store = new ActiveSessionStore();
        uint userId = 42;
        string stamp = "user-session-stamp";

        // Act
        store.RegisterSession(userId, stamp);
        store.InvalidateSession(userId, SessionInvalidationReason.LoggedOut);
        var (isValid, reason) = store.IsSessionValid(userId, stamp);

        // Assert
        Assert.False(isValid);
        Assert.Equal(SessionInvalidationReason.LoggedOut, reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsSessionValid_NullOrWhitespaceStamp_ReturnsFalseWithReplacedByNewDevice(string? invalidStamp)
    {
        // Arrange
        var store = new ActiveSessionStore();
        uint userId = 99;
        store.RegisterSession(userId, "valid-stamp");

        // Act
        var (isValid, reason) = store.IsSessionValid(userId, invalidStamp!);

        // Assert
        Assert.False(isValid);
        Assert.Equal(SessionInvalidationReason.ReplacedByNewDevice, reason);
    }

    [Fact]
    public void IsSessionValid_UserNotRegistered_ReturnsFalseWithReplacedByNewDevice()
    {
        // Arrange
        var store = new ActiveSessionStore();

        // Act
        var (isValid, reason) = store.IsSessionValid(999, "any-stamp");

        // Assert
        Assert.False(isValid);
        Assert.Equal(SessionInvalidationReason.ReplacedByNewDevice, reason);
    }
}
