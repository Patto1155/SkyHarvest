// WeatherStateMachine tests (plan Task 11, adapted to pure-C# WeatherStateMachine).
using NUnit.Framework;
using SkyHarvest.Weather;
using SkyHarvest.Core;

public class WeatherStateMachineTests
{
    [SetUp]
    public void SetUp() => EventBus.Clear();

    [Test]
    public void Initial_State_Is_ClearSkies()
    {
        var sm = new WeatherStateMachine(WeatherType.ClearSkies);
        Assert.AreEqual(WeatherType.ClearSkies, sm.CurrentWeather);
    }

    [Test]
    public void ForceTransition_Changes_State()
    {
        var sm = new WeatherStateMachine(WeatherType.ClearSkies);
        sm.ForceTransition(WeatherType.LightRain);
        Assert.AreEqual(WeatherType.LightRain, sm.CurrentWeather);
    }

    [Test]
    public void Transition_Publishes_WeatherChangedEvent()
    {
        var sm = new WeatherStateMachine(WeatherType.ClearSkies);
        WeatherType received = WeatherType.ClearSkies;
        EventBus.Subscribe<WeatherChangedEvent>(e => received = e.Current);
        sm.ForceTransition(WeatherType.HeavyStorm);
        Assert.AreEqual(WeatherType.HeavyStorm, received);
    }

    [Test]
    public void TimeRemaining_Decreases_On_Tick()
    {
        var sm = new WeatherStateMachine(WeatherType.ClearSkies, seed: 42);
        float initial = sm.MinutesRemaining;
        sm.Tick(2f);
        Assert.Less(sm.MinutesRemaining, initial);
    }

    [Test]
    public void Tick_Transitions_When_Time_Expires()
    {
        var sm = new WeatherStateMachine(WeatherType.ClearSkies, seed: 1);
        bool changed = false;
        EventBus.Subscribe<WeatherChangedEvent>(e => changed = true);
        // Drive time to zero
        sm.Tick(sm.MinutesRemaining + 0.1f);
        Assert.IsTrue(changed);
    }

    [Test]
    public void SetState_Restores_Weather_And_Duration()
    {
        var sm = new WeatherStateMachine(WeatherType.ClearSkies);
        sm.SetState(WeatherType.FogBank, 3.0f);
        Assert.AreEqual(WeatherType.FogBank, sm.CurrentWeather);
        Assert.AreEqual(3.0f, sm.MinutesRemaining, 0.001f);
    }

    [Test]
    public void NextWeatherHint_Null_When_Far_From_Transition()
    {
        var sm = new WeatherStateMachine(WeatherType.ClearSkies, seed: 42);
        // Duration is at least MinWeatherDurationMinutes = 5; hint only fires at <= 1.5
        // Immediately after creation, MinutesRemaining > 1.5
        sm.Tick(0.1f);
        // Should be null unless duration was very short (seed-dependent)
        if (sm.MinutesRemaining > 1.5f)
            Assert.IsNull(sm.NextWeatherHint);
    }

    [Test]
    public void NextWeatherHint_Set_When_Within_1_5_Min()
    {
        var sm = new WeatherStateMachine(WeatherType.ClearSkies, seed: 0);
        // Drive to just above 1.5 min then verify hint appears at <= 1.5
        float toTick = sm.MinutesRemaining - 1.4f;
        if (toTick > 0f) sm.Tick(toTick);
        // Now MinutesRemaining should be ~1.4 → hint should be set
        Assert.IsNotNull(sm.NextWeatherHint);
    }

    [Test]
    public void ForceTransition_Same_State_Still_Publishes_Not()
    {
        // ForceTransition to the same state should NOT publish (no state change)
        var sm = new WeatherStateMachine(WeatherType.ClearSkies);
        int evtCount = 0;
        EventBus.Subscribe<WeatherChangedEvent>(e => evtCount++);
        sm.ForceTransition(WeatherType.ClearSkies);
        // Same weather → event skipped
        Assert.AreEqual(0, evtCount);
    }
}
