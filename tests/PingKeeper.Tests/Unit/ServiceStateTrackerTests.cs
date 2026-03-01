using FluentAssertions;
using PingKeeper.Models;
using PingKeeper.Services;

namespace PingKeeper.Tests.Unit;

[Trait("Category", "Unit")]
public class ServiceStateTrackerTests
{
    private readonly ServiceStateTracker _sut = new();

    [Fact]
    public void GetOrCreate_NewEndpoint_CreatesState()
    {
        var endpoint = new ServiceEndpoint { Name = "Test", Url = "http://test.local" };

        var state = _sut.GetOrCreate(endpoint);

        state.Should().NotBeNull();
        state.ServiceName.Should().Be("Test");
        state.ServiceUrl.Should().Be("http://test.local");
        state.IsDown.Should().BeFalse();
    }

    [Fact]
    public void GetOrCreate_SameUrl_ReturnsSameInstance()
    {
        var endpoint = new ServiceEndpoint { Name = "Test", Url = "http://test.local" };

        var state1 = _sut.GetOrCreate(endpoint);
        var state2 = _sut.GetOrCreate(endpoint);

        state1.Should().BeSameAs(state2);
    }

    [Fact]
    public void GetOrCreate_DifferentUrls_ReturnsDifferentInstances()
    {
        var endpoint1 = new ServiceEndpoint { Name = "A", Url = "http://a.local" };
        var endpoint2 = new ServiceEndpoint { Name = "B", Url = "http://b.local" };

        var state1 = _sut.GetOrCreate(endpoint1);
        var state2 = _sut.GetOrCreate(endpoint2);

        state1.Should().NotBeSameAs(state2);
    }

    [Fact]
    public void GetOrCreate_PreservesStateMutations()
    {
        var endpoint = new ServiceEndpoint { Name = "Test", Url = "http://test.local" };

        var state = _sut.GetOrCreate(endpoint);
        state.RecordFailure("err", threshold: 1);

        var sameState = _sut.GetOrCreate(endpoint);

        sameState.IsDown.Should().BeTrue();
        sameState.ConsecutiveFailures.Should().Be(1);
    }
}
