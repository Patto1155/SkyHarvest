using System;
using System.Collections.Generic;

namespace SkyHarvest.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = new List<Delegate>();
            _subscribers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_subscribers.ContainsKey(type))
                _subscribers[type].Remove(handler);
        }

        public static void Publish<T>(T evt)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type)) return;
            foreach (var handler in _subscribers[type].ToArray())
                ((Action<T>)handler)(evt);
        }

        public static void Clear() => _subscribers.Clear();
    }
}
