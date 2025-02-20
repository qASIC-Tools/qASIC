using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace qASIC
{
    public class qServices : IEnumerable<object>
    {
        public qServices(qInstance instance)
        {
            Instance = instance;
        }

        public qInstance Instance { get; private set; }
        private List<object> items = new List<object>();

        public object Get(Type type) =>
            items.Where(x => x.GetType() == type)
            .FirstOrDefault();

        public T Get<T>() =>
            items.Where(x => x is T)
            .Select(x => (T)x)
            .FirstOrDefault();

        public IEnumerable<object> GetMultiple(Type type) =>
            items.Where(x => x.GetType() == type);

        public IEnumerable<T> GetMultiple<T>() =>
            items.Where(x => x is T)
            .Select(x => (T)x);

        public qServices Add(object obj)
        {
            items.Add(obj);
            if (obj is IService service)
                service.Instance = Instance;

            return this;
        }

        public qServices Remove(object obj)
        {
            items.Remove(obj);
            if (obj is IService service)
                service.Instance = null;

            return this;
        }

        public IEnumerator<object> GetEnumerator() =>
            items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
