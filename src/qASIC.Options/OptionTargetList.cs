using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

namespace qASIC.Options
{
    /// <summary>List of found option targets that were marked with an <see cref="OptionAttribute"/>.</summary>
    public class OptionTargetList : IEnumerable<KeyValuePair<string, OptionTargetList.Target>>, IService
    {
        public OptionTargetList(OptionsManager manager = null) : this(null, manager) { }
        public OptionTargetList(qInstance instance, OptionsManager manager = null)
        {
            Instance = instance;
            Manager = manager;

            RegisteredObjects = new qRegisteredObjects();
            RegisteredObjects.OnObjectRegistered += LoadValuesForObject;
        }

        public BindingFlags Flags { get; set; } = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        Dictionary<string, List<Target>> Targets { get; set; } = new Dictionary<string, List<Target>>();

        public qRegisteredObjects RegisteredObjects { get; private set; }

        public qInstance Instance { get; set; }

        private OptionsManager manager;
        public OptionsManager Manager
        {
            get => manager;
            set
            {
                if (manager != null)
                    manager.OnOptionChanged.mainAction -= Manager_OnOptionChanged;

                manager = value;

                if (manager != null)
                    manager.OnOptionChanged.mainAction += Manager_OnOptionChanged;
            }
        }

        public List<Target> this[string name]
        {
            get
            {
                name = OptionsManager.FormatKeyString(name);

                if (!Targets.ContainsKey(name))
                    Targets.Add(name, new List<Target>());

                return Targets[name];
            }
        }

        private void Manager_OnOptionChanged(string key, ChangeOptionArgs args)
        {
            if (!Targets.ContainsKey(args.optionName)) return;
            Set(args.optionName, args.value);
        }

        /// <summary>Finds and adds options to the list.</summary>
        /// <returns>Returns itself.</returns>
        public OptionTargetList FindOptions() =>
            FindOptions<OptionAttribute>();

        /// <summary>Finds and adds options marked in a class to the list.</summary>
        /// <typeparam name="TClass">Class to search.</typeparam>
        /// <returns>Returns itself.</returns>
        public OptionTargetList FindOptionsInClass<TClass>() where TClass : class =>
            FindOptionsInClass<TClass, OptionAttribute>();

        /// <summary>Finds and adds options marked with the specified attribute in a class to the list.</summary>
        /// <typeparam name="TClass">Class to search.</typeparam>
        /// <typeparam name="TOption">Attribute for marking options.</typeparam>
        /// <returns>Returns itself.</returns>
        public OptionTargetList FindOptionsInClass<TClass, TOption>() where TClass : class
            where TOption : OptionAttribute
        {
            var methods = TypeFinder.FindMethodsWithAttributeInClass<TClass, TOption>(Flags);
            var properties = TypeFinder.FindPropertiesWithAttributeInClass<TClass, TOption>(Flags);
            var fields = TypeFinder.FindFieldsWithAttributeInClass<TClass, TOption>(Flags);

            CreateTargets<TOption>(methods, properties, fields);
            return this;
        }

        /// <summary>Finds and adds options marked with the specified attribute to the list.</summary>
        /// <typeparam name="TOption">Attribute for marking options.</typeparam>
        /// <returns>Returns itself.</returns>
        public OptionTargetList FindOptions<TOption>() where TOption : OptionAttribute
        {
            var methods = TypeFinder.FindMethodsWithAttribute<TOption>(Flags);
            var properties = TypeFinder.FindPropertiesWithAttribute<TOption>(Flags);
            var fields = TypeFinder.FindFieldsWithAttribute<TOption>(Flags);

            CreateTargets<TOption>(methods, properties, fields);
            return this;
        }

        private void CreateTargets<TOption>(IEnumerable<MethodInfo> methods, IEnumerable<PropertyInfo> properties, IEnumerable<FieldInfo> fields)
            where TOption : OptionAttribute
        {
            foreach (var item in methods)
            {
                var attr = item.GetCustomAttribute<TOption>();
                AddTarget(attr.Name ?? item.Name, new MethodTarget(item)
                {
                    HasDefaultValue = attr.HasDefaultValue,
                    DefaultValue = attr.DefaultValue,
                });
            }

            foreach (var item in properties)
            {
                var attr = item.GetCustomAttribute<TOption>();
                AddTarget(attr.Name ?? item.Name, new PropertyTarget(item)
                {
                    HasDefaultValue = attr.HasDefaultValue,
                    DefaultValue = attr.DefaultValue,
                });
            }

            foreach (var item in fields)
            {
                var attr = item.GetCustomAttribute<TOption>();
                AddTarget(attr.Name ?? item.Name, new FieldTarget(item)
                {
                    HasDefaultValue = attr.HasDefaultValue,
                    DefaultValue = attr.DefaultValue,
                });
            }
        }

        public OptionTargetList PopulateManagerFromTargets()
        {
            if (Manager == null)
                throw new NullReferenceException("You have to assign an Options Manager to populate it's list!");

            foreach (var item in Targets)
            {
                if (Manager.OptionsList.ContainsKey(item.Key)) continue;

                if (!TryGetDefalutValue(item.Key, out var defaultValue))
                    defaultValue = null;

                Manager.SetOption(item.Key, defaultValue);
            }

            return this;
        }

        void AddTarget(string key, Target target)
        {
            key = OptionsManager.FormatKeyString(key);

            if (!Targets.ContainsKey(key))
                Targets.Add(key, new List<Target>());

            Targets[key].Add(target);
        }

        /// <summary>Gets a default value for an option.</summary>
        /// <param name="name">Name of the option.</param>
        /// <param name="value">The default value.</param>
        /// <returns>If a default value was able to be extracted.</returns>
        bool TryGetDefalutValue(string name, out object value)
        {
            name = OptionsManager.FormatKeyString(name);
            value = null;

            var items = this[name];

            foreach (var item in items)
            {
                if (!item.HasDefaultValue) continue;

                value = item.DefaultValue;
                return true;
            }

            foreach (var item in items)
            {
                if (!item.CanGetValue) continue;

                if (item.IsStatic)
                {
                    try
                    {
                        value = item.GetValue(null);
                        return true;
                    }
                    catch { }
                    continue;
                }

                var targets = RegisteredObjects.Where(x => x?.GetType() == item.DeclaringType);

                foreach (var obj in targets)
                {
                    try
                    {
                        var val = item.GetValue(obj);
                        value = val;
                        return true;
                    }
                    catch { }
                }
            }

            foreach (var item in items)
            {
                try
                {
                    value = TypeFinder.CreateConstructorFromType(item.ValueType);
                    return true;
                }
                catch { }
            }

            return false;
        }

        /// <summary>Sets a value for an option.</summary>
        /// <param name="registeredObjects">List of registered objects.</param>
        /// <param name="name">Name of the option.</param>
        /// <param name="value">Value to set.</param>
        void Set(string name, object value)
        {
            name = OptionsManager.FormatKeyString(name);

            var items = this[name];

            var args = new ChangeOptionArgs()
            {
                optionName = name,
                value = value,
            };

            foreach (var item in items)
            {
                if (item.IsStatic)
                {
                    try
                    {
                        item.SetValue(null, args);
                    }
                    catch { }

                    continue;
                }

                var targets = RegisteredObjects.Where(x => x?.GetType() == item.DeclaringType);

                foreach (var obj in targets)
                {
                    try
                    {
                        item.SetValue(obj, args);
                    }
                    catch { }
                }
            }
        }

        /// <summary>Loads values of options for an object.</summary>
        /// <param name="obj">Object to load values for.</param>
        public void LoadValuesForObject(object obj)
        {
            var type = obj.GetType();

            foreach (var target in Targets)
            {
                if (!Manager.OptionsList.TryGetValue(target.Key, out var val)) continue;

                var args = new ChangeOptionArgs()
                {
                    optionName = val.name,
                    value = val.value,
                };

                foreach (var item in target.Value)
                {
                    if (item.DeclaringType != type) continue;
                    item.SetValue(obj, args);
                }
            }
        }

        public IEnumerator<KeyValuePair<string, Target>> GetEnumerator() =>
            Targets
            .SelectMany(x => x.Value.Select(y => new KeyValuePair<string, Target>(x.Key, y)))
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public abstract class Target
        {
            public abstract bool IsStatic { get; }
            public abstract Type ValueType { get; }
            public abstract Type DeclaringType { get; }

            public bool HasDefaultValue { get; set; }
            public object DefaultValue { get; set; }

            public void SetValue(qRegisteredObjects registeredObjects, ChangeOptionArgs args)
            {
                foreach (var item in registeredObjects.Where(x => x.GetType() == DeclaringType))
                    SetValue(item, args);
            }

            public abstract void SetValue(object obj, ChangeOptionArgs args);

            public virtual bool CanGetValue => true;

            public virtual object GetValue(object obj) =>
                null;
        }

        public class MethodTarget : Target
        {
            public MethodTarget(MethodInfo method)
            {
                Method = method;
                parameters = method.GetParameters();
            }

            public MethodInfo Method { get; private set; }

            ParameterInfo[] parameters;

            public override bool IsStatic => Method.IsStatic;
            public override bool CanGetValue => false;

            public override Type ValueType
            {
                get
                {
                    if (parameters.Length == 0)
                        return null;

                    if (parameters.Length == 1)
                        return parameters[0].ParameterType;

                    return parameters[1].ParameterType;
                }
            }

            public override Type DeclaringType => Method.DeclaringType;

            public override void SetValue(object obj, ChangeOptionArgs args)
            {
                switch (parameters.Length)
                {
                    case 0:
                        Method.Invoke(obj, parameters);
                        break;
                    case 1:
                        Method.Invoke(obj, new object[] { parameters[0].ParameterType == typeof(ChangeOptionArgs) ?
                            args :
                            args.value });
                        break;
                    case 2:
                        Method.Invoke(obj, new object[]
                        {
                            args,
                            args.value,
                        });
                        break;
                }
            }
        }

        public class PropertyTarget : Target
        {
            public PropertyTarget(PropertyInfo property)
            {
                Property = property;
            }

            public PropertyInfo Property { get; private set; }

            public override bool IsStatic => Property.GetAccessors(nonPublic: true)[0].IsStatic;
            public override bool CanGetValue => Property.CanRead;

            public override Type ValueType => Property.PropertyType;
            public override Type DeclaringType => Property.DeclaringType;

            public override object GetValue(object obj) =>
                Property.GetValue(obj);

            public override void SetValue(object obj, ChangeOptionArgs args) =>
                Property.SetValue(obj, args.value);
        }

        public class FieldTarget : Target
        {
            public FieldTarget(FieldInfo field)
            {
                Field = field;
            }

            public FieldInfo Field { get; private set; }

            public override bool IsStatic => Field.IsStatic;

            public override Type ValueType => Field.FieldType;
            public override Type DeclaringType => Field.DeclaringType;

            public override object GetValue(object obj) =>
                Field.GetValue(obj);

            public override void SetValue(object obj, ChangeOptionArgs args) =>
                Field.SetValue(obj, args.value);
        }
    }
}
