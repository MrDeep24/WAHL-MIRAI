using System;
using System.Threading.Tasks;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests;

public class LoginAttemptStoreTests
{
    [Fact]
    public void RegisterFailure_LadderProgression_AppliesCorrectDurations()
    {
        var startTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(startTime);
        var store = new LoginAttemptStore(timeProvider);
        const string docHash = "hash-ladder";

        // Failures 1 to 4: no lock
        for (int i = 1; i <= 4; i++)
        {
            var res = store.RegisterFailure(docHash);
            Assert.Equal(i, res.FailedCount);
            Assert.Null(res.LockedUntilUtc);
            Assert.False(res.JustLockedOut);
            Assert.False(store.IsLockedOut(docHash, out _));
        }

        // 5th failure: ladder step 1 -> 1 min lock
        var res5 = store.RegisterFailure(docHash);
        Assert.Equal(5, res5.FailedCount);
        Assert.True(res5.JustLockedOut);
        Assert.NotNull(res5.LockedUntilUtc);
        Assert.Equal(startTime.AddMinutes(1), res5.LockedUntilUtc.Value);
        Assert.True(store.IsLockedOut(docHash, out var rem5));
        Assert.True(rem5.TotalSeconds <= 60 && rem5.TotalSeconds > 59);

        // Advance 61 seconds (lock expires, still within 15-min window)
        timeProvider.Advance(TimeSpan.FromSeconds(61));
        Assert.False(store.IsLockedOut(docHash, out _));

        // 6th failure: ladder step 2 -> 5 min lock
        var res6 = store.RegisterFailure(docHash);
        Assert.Equal(6, res6.FailedCount);
        Assert.True(res6.JustLockedOut);
        Assert.Equal(timeProvider.GetUtcNow().AddMinutes(5), res6.LockedUntilUtc);
        Assert.True(store.IsLockedOut(docHash, out _));

        // Advance 5 min + 1 sec
        timeProvider.Advance(TimeSpan.FromSeconds(301));
        Assert.False(store.IsLockedOut(docHash, out _));

        // 7th failure: ladder step 3 -> 15 min lock
        var res7 = store.RegisterFailure(docHash);
        Assert.Equal(7, res7.FailedCount);
        Assert.True(res7.JustLockedOut);
        Assert.Equal(timeProvider.GetUtcNow().AddMinutes(15), res7.LockedUntilUtc);
        Assert.True(store.IsLockedOut(docHash, out _));

        // Advance 15 min + 1 sec
        timeProvider.Advance(TimeSpan.FromSeconds(901));
        Assert.False(store.IsLockedOut(docHash, out _));

        // 8th failure: stays at 15 min
        var res8 = store.RegisterFailure(docHash);
        Assert.Equal(8, res8.FailedCount);
        Assert.True(res8.JustLockedOut);
        Assert.Equal(timeProvider.GetUtcNow().AddMinutes(15), res8.LockedUntilUtc);
        Assert.True(store.IsLockedOut(docHash, out _));
    }

    [Fact]
    public void RegisterFailure_WindowExpiry_ResetsCount()
    {
        var startTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(startTime);
        var store = new LoginAttemptStore(timeProvider);
        const string docHash = "hash-window-reset";

        // 4 failures within window
        for (int i = 1; i <= 4; i++)
        {
            store.RegisterFailure(docHash);
        }

        // Advance time by 16 minutes (> 15-min window)
        timeProvider.Advance(TimeSpan.FromMinutes(16));

        // Next failure should reset count and start at 1, no lockout
        var result = store.RegisterFailure(docHash);
        Assert.Equal(1, result.FailedCount);
        Assert.Null(result.LockedUntilUtc);
        Assert.False(result.JustLockedOut);
        Assert.False(store.IsLockedOut(docHash, out _));
    }

    [Fact]
    public void RegisterSuccess_ClearsExistingFailureRecords()
    {
        var startTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(startTime);
        var store = new LoginAttemptStore(timeProvider);
        const string docHash = "hash-success-clears";

        // Record 4 failures
        for (int i = 0; i < 4; i++)
        {
            store.RegisterFailure(docHash);
        }

        // User succeeds
        store.RegisterSuccess(docHash);

        // Next failure starts at 1
        var result = store.RegisterFailure(docHash);
        Assert.Equal(1, result.FailedCount);
        Assert.False(store.IsLockedOut(docHash, out _));
    }

    [Fact]
    public void IsLockedOut_LockExpiry_ReturnsFalseAfterDurationPasses()
    {
        var startTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(startTime);
        var store = new LoginAttemptStore(timeProvider);
        const string docHash = "hash-lock-expiry";

        // Reach 5 failures -> locked for 1 min
        for (int i = 0; i < 5; i++)
        {
            store.RegisterFailure(docHash);
        }

        Assert.True(store.IsLockedOut(docHash, out var remaining));
        Assert.True(remaining > TimeSpan.Zero);

        // Advance 60 seconds + 1 millisecond
        timeProvider.Advance(TimeSpan.FromMilliseconds(60001));

        Assert.False(store.IsLockedOut(docHash, out var expiredRemaining));
        Assert.Equal(TimeSpan.Zero, expiredRemaining);
    }

    [Fact]
    public void LazyPurge_WhenCountExceeds5000_RemovesExpiredEntries()
    {
        var startTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(startTime);
        var store = new LoginAttemptStore(timeProvider);

        // Add 5001 entries at t=0
        for (int i = 1; i <= 5001; i++)
        {
            store.RegisterFailure($"doc-{i}");
        }

        // Advance 16 minutes so their window has expired
        timeProvider.Advance(TimeSpan.FromMinutes(16));

        // Register one more failure to trigger lazy purge
        store.RegisterFailure("trigger-purge");

        // The older expired entries should have been purged (checking one previously added)
        Assert.False(store.IsLockedOut("doc-1", out _));
    }

    [Fact]
    public void Concurrency_1000ParallelFailuresOnSameKey_CountsAllFailures()
    {
        var startTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(startTime);
        var store = new LoginAttemptStore(timeProvider);
        const string docHash = "hash-concurrent";

        Parallel.For(0, 1000, _ =>
        {
            store.RegisterFailure(docHash);
        });

        // The last registered count must be 1000 and the account must be locked
        Assert.True(store.IsLockedOut(docHash, out _));
        // Verify with another failure
        var result = store.RegisterFailure(docHash);
        Assert.Equal(1001, result.FailedCount);
    }
}
