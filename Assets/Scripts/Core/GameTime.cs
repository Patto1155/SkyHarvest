// Assets/Scripts/Core/GameTime.cs
// Owned by: world/island agent
namespace SkyHarvest.Core
{
    /// <summary>
    /// Pure-logic game clock. Ticked every frame by GameManager.Update.
    /// All simulation listens to GameTickEvent, not directly to this class.
    /// </summary>
    public class GameTimeClock
    {
        public float TotalMinutes { get; private set; }

        public int CurrentHour =>
            (int)(TotalMinutes / Constants.MinutesPerGameHour) % (int)Constants.HoursPerGameDay;

        public int CurrentDay =>
            (int)(TotalMinutes / (Constants.MinutesPerGameHour * Constants.HoursPerGameDay));

        private int _lastHour = -1;
        private int _lastDay  = -1;

        public void Tick(float realDeltaSeconds)
        {
            float deltaMinutes = realDeltaSeconds / Constants.SecondsPerGameMinute;
            TotalMinutes += deltaMinutes;

            EventBus.Publish(new GameTickEvent
            {
                DeltaMinutes      = deltaMinutes,
                TotalGameMinutes  = TotalMinutes
            });

            int hour = CurrentHour;
            if (_lastHour != -1 && hour != _lastHour)
                EventBus.Publish(new HourChangedEvent { Hour = hour });
            _lastHour = hour;

            int day = CurrentDay;
            if (_lastDay != -1 && day != _lastDay)
                EventBus.Publish(new DayChangedEvent { Day = day });
            _lastDay = day;
        }

        /// <summary>Restore clock from a saved total-minutes value (save/load).</summary>
        public void SetTime(float totalMinutes)
        {
            TotalMinutes = totalMinutes;
            _lastHour    = CurrentHour;
            _lastDay     = CurrentDay;
        }
    }
}
