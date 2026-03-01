using FluentAssertions;
using PingKeeper.Models;

namespace PingKeeper.Tests.Unit;

[Trait("Category", "Unit")]
public class ServiceStateTests
{
    private readonly ServiceState _sut = new("TestService", "http://test.local");

    [Fact]
    public void NewState_IsUp_WithZeroFailures()
    {
        _sut.IsDown.Should().BeFalse();
        _sut.ConsecutiveFailures.Should().Be(0);
        _sut.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RecordSuccess_WhenUp_ReturnsFalse()
    {
        var recovered = _sut.RecordSuccess();

        recovered.Should().BeFalse();
        _sut.IsDown.Should().BeFalse();
        _sut.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void RecordFailure_BelowThreshold_ReturnsFalse()
    {
        var wentDown = _sut.RecordFailure("error", threshold: 3);

        wentDown.Should().BeFalse();
        _sut.IsDown.Should().BeFalse();
        _sut.ConsecutiveFailures.Should().Be(1);
        _sut.LastErrorMessage.Should().Be("error");
    }

    [Fact]
    public void RecordFailure_AtThreshold_ReturnsTrue()
    {
        _sut.RecordFailure("err1", threshold: 3);
        _sut.RecordFailure("err2", threshold: 3);
        var wentDown = _sut.RecordFailure("err3", threshold: 3);

        wentDown.Should().BeTrue();
        _sut.IsDown.Should().BeTrue();
        _sut.ConsecutiveFailures.Should().Be(3);
    }

    [Fact]
    public void RecordFailure_AboveThreshold_ReturnsFalse_AlreadyDown()
    {
        for (int i = 0; i < 3; i++)
            _sut.RecordFailure($"err{i}", threshold: 3);

        var wentDownAgain = _sut.RecordFailure("err4", threshold: 3);

        wentDownAgain.Should().BeFalse();
        _sut.IsDown.Should().BeTrue();
        _sut.ConsecutiveFailures.Should().Be(4);
    }

    [Fact]
    public void RecordSuccess_WhenDown_ReturnsTrue_Recovery()
    {
        for (int i = 0; i < 3; i++)
            _sut.RecordFailure($"err{i}", threshold: 3);
        _sut.IsDown.Should().BeTrue();

        var recovered = _sut.RecordSuccess();

        recovered.Should().BeTrue();
        _sut.IsDown.Should().BeFalse();
        _sut.ConsecutiveFailures.Should().Be(0);
        _sut.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RecordSuccess_AfterRecovery_ReturnsFalse()
    {
        for (int i = 0; i < 3; i++)
            _sut.RecordFailure($"err{i}", threshold: 3);
        _sut.RecordSuccess();

        var recoveredAgain = _sut.RecordSuccess();

        recoveredAgain.Should().BeFalse();
    }

    [Fact]
    public void FullCycle_UpDownUpDown()
    {
        // Up → Down
        for (int i = 0; i < 3; i++)
            _sut.RecordFailure("err", threshold: 3);
        _sut.IsDown.Should().BeTrue();

        // Down → Up (recovery)
        _sut.RecordSuccess().Should().BeTrue();
        _sut.IsDown.Should().BeFalse();

        // Up → Down again
        for (int i = 0; i < 3; i++)
            _sut.RecordFailure("err", threshold: 3);
        _sut.IsDown.Should().BeTrue();

        // Down → Up again
        _sut.RecordSuccess().Should().BeTrue();
        _sut.IsDown.Should().BeFalse();
    }

    [Fact]
    public void RecordSuccess_ResetsConsecutiveFailures()
    {
        _sut.RecordFailure("err1", threshold: 5);
        _sut.RecordFailure("err2", threshold: 5);
        _sut.ConsecutiveFailures.Should().Be(2);

        _sut.RecordSuccess();

        _sut.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void RecordFailure_WithThresholdOne_GoesDownImmediately()
    {
        var wentDown = _sut.RecordFailure("err", threshold: 1);

        wentDown.Should().BeTrue();
        _sut.IsDown.Should().BeTrue();
        _sut.ConsecutiveFailures.Should().Be(1);
    }

    [Fact]
    public void Properties_ReturnConstructorValues()
    {
        _sut.ServiceName.Should().Be("TestService");
        _sut.ServiceUrl.Should().Be("http://test.local");
    }
}
