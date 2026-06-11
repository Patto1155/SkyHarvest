// Assets/Tests/EditMode/EventBusTests.cs
// Smoke tests for EventBus — ported from plan Task 2.
// Owned by: harness agent.
using NUnit.Framework;
using SkyHarvest.Core;

[TestFixture]
public class EventBusTests
{
    struct TestEvent { public int Value; }
    struct OtherEvent { public string Name; }

    [SetUp]
    public void SetUp() => EventBus.Clear();

    [Test]
    public void Subscribe_And_Publish_Delivers_Event()
    {
        int received = 0;
        EventBus.Subscribe<TestEvent>(e => received = e.Value);
        EventBus.Publish(new TestEvent { Value = 42 });
        Assert.AreEqual(42, received);
    }

    [Test]
    public void Unsubscribe_Stops_Delivery()
    {
        int received = 0;
        void Handler(TestEvent e) => received = e.Value;
        EventBus.Subscribe<TestEvent>(Handler);
        EventBus.Unsubscribe<TestEvent>(Handler);
        EventBus.Publish(new TestEvent { Value = 99 });
        Assert.AreEqual(0, received);
    }

    [Test]
    public void Different_Event_Types_Are_Independent()
    {
        int intReceived = 0;
        string? strReceived = null;
        EventBus.Subscribe<TestEvent>(e => intReceived = e.Value);
        EventBus.Subscribe<OtherEvent>(e => strReceived = e.Name);
        EventBus.Publish(new TestEvent { Value = 7 });
        Assert.AreEqual(7, intReceived);
        Assert.IsNull(strReceived);
    }

    [Test]
    public void Multiple_Subscribers_All_Receive()
    {
        int count = 0;
        EventBus.Subscribe<TestEvent>(_ => count++);
        EventBus.Subscribe<TestEvent>(_ => count++);
        EventBus.Subscribe<TestEvent>(_ => count++);
        EventBus.Publish(new TestEvent { Value = 1 });
        Assert.AreEqual(3, count);
    }

    [Test]
    public void Clear_Removes_All_Subscribers()
    {
        int received = 0;
        EventBus.Subscribe<TestEvent>(e => received = e.Value);
        EventBus.Clear();
        EventBus.Publish(new TestEvent { Value = 55 });
        Assert.AreEqual(0, received);
    }

    [Test]
    public void Publish_With_No_Subscribers_Does_Not_Throw()
    {
        Assert.DoesNotThrow(() => EventBus.Publish(new OtherEvent { Name = "hello" }));
    }

    [Test]
    public void Publish_Delivers_Correct_Value_In_Struct()
    {
        WeatherChangedEvent received = default;
        EventBus.Subscribe<WeatherChangedEvent>(e => received = e);
        EventBus.Publish(new WeatherChangedEvent
        {
            Previous = WeatherType.ClearSkies,
            Current = WeatherType.HeavyStorm
        });
        Assert.AreEqual(WeatherType.ClearSkies, received.Previous);
        Assert.AreEqual(WeatherType.HeavyStorm, received.Current);
    }
}
