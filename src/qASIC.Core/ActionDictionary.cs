using System.Collections.Generic;
using System;

namespace qASIC
{
    public class ActionDictionary<TKey>
    {
        Dictionary<TKey, Action> keyActions = new Dictionary<TKey, Action>();
        public Action mainAction;

        public Action this[TKey key]
        {
            get
            {
                if (!keyActions.ContainsKey(key))
                    keyActions.Add(key, () => { });

                return keyActions[key];
            }
        }

        public void Invoke(TKey key)
        {
            if (keyActions.ContainsKey(key))
                keyActions[key]?.Invoke();
        }

        public void InvokeAll()
        {
            foreach (var item in keyActions)
                item.Value?.Invoke();
        }

        public static implicit operator Action(ActionDictionary<TKey> dict) =>
            dict.mainAction;
    }

    public class ActionDictionary<TKey, TArg>
    {
        Dictionary<TKey, Action<TArg>> actions = new Dictionary<TKey, Action<TArg>>();
        public Action<TArg> mainAction;

        public Action<TArg> this[TKey key]
        {
            get
            {
                if (!actions.ContainsKey(key))
                    actions.Add(key, _ => { });

                return actions[key];
            }
        }

        public void Invoke(TKey key, TArg arg)
        {
            if (actions.ContainsKey(key))
                actions[key]?.Invoke(arg);
        }

        public void InvokeAll(TKey key, TArg arg)
        {
            foreach (var item in actions)
                item.Value?.Invoke(arg);
        }

        public static implicit operator Action<TArg>(ActionDictionary<TKey, TArg> dict) =>
            dict.mainAction;
    }
}