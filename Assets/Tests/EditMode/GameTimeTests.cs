// Assets/Tests/EditMode/GameTimeTests.cs
// Verbatim from plan Task 3, adapted to use our SetTime helper.
using NUnit.Framework;
using SkyHarvest.Core;

public class GameTimeTests
{
    [SetUp]
    public void SetUp() => EventBus.Clear();

    [Test]
    public void Tick_Advances_TotalMinutes()
    {
        var time = new GameTimeClock();
        time.Tick(Constants.SecondsPerGameMinute * 5f);
        Assert.AreEqual(5f, time.TotalMinutes, 0.01f);
    }

    [Test]
    public void Tick_Publishes_GameTickEvent()
    {
        var time = new GameTimeClock();
        float received = 0f;
        EventBus.Subscribe<GameTickEvent>(e => received = e.DeltaMinutes);
        time.Tick(Constants.SecondsPerGameMinute * 3f);
        Assert.AreEqual(3f, received, 0.01f);
    }

    [Test]
    public void Hour_Change_Publishes_Event()
    {
        var time = new GameTimeClock();
        // Prime the _lastHour tracking with an initial tick
        time.Tick(0.001f);
        int hourReceived = -1;
        EventBus.Subscribe<HourChangedEvent>(e => hourReceived = e.Hour);
        // Advance 60 game minutes (= 1 hour)
        time.Tick(Constants.SecondsPerGameMinute * Constants.MinutesPerGameHour);
        Assert.AreEqual(1, hourReceived);
    }

    [Test]
    public void CurrentHour_Wraps_At_24()
    {
        var time = new GameTimeClock();
        float fullDay = Constants.SecondsPerGameMinute
                      * Constants.MinutesPerGameHour
                      * Constants.HoursPerGameDay;
        time.Tick(fullDay);
        Assert.AreEqual(0, time.CurrentHour);
        Assert.AreEqual(1, time.CurrentDay);
    }

    [Test]
    public void SetTime_Restores_TotalMinutes()
    {
        var time = new GameTimeClock();
        time.SetTime(120f);
        Assert.AreEqual(120f, time.TotalMinutes, 0.001f);
        Assert.AreEqual(2, time.CurrentHour);   // 120 min / 60 = hour 2
    }
}
