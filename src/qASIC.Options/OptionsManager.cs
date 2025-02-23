using System;
using System.Linq;
using qASIC.Options.Serialization;

namespace qASIC.Options
{
    public class OptionsManager : IService, IHasLogs
    {
        public OptionsManager(OptionsSerializer serializer = null) :
            this(new OptionsList(), serializer: serializer)
        { }

        public OptionsManager(OptionsList optionsList, OptionsSerializer serializer = null)
        {
            OptionsList = optionsList;
            Serializer = serializer ?? new qARKOptionsSerializer();
        }

        /// <summary>Main static instance of <see cref="OptionsManager"/> that was set using <see cref="SetAsMain"/>.</summary>
        public static OptionsManager Main { get; private set; }

        /// <summary>Sets this instance as main to make it accessible from property <see cref="Main"/>.</summary>
        /// <returns>Returns itself.</returns>
        public OptionsManager SetAsMain()
        {
            Main = this;
            return this;
        }

        public LogManager Logs { get; set; } = new LogManager();

        public OptionsSerializer Serializer { get; set; }

        /// <summary>List containing options and their values.</summary>
        public OptionsList OptionsList { get; private set; }

        public qInstance Instance { get; set; }

        /// <summary>Initializes the options manager.</summary>
        /// <param name="log">If the change should be logged.</param>
        public void Initialize(bool log = true)
        {
            Revert(log);
            Apply(log);

            if (log)
                Logs.Log("Settings initialized!", "settings_init");
        }

        /// <summary>Formats a <c>string</c> to be used as a key for an option.</summary>
        /// <param name="text">String to format.</param>
        /// <returns>The formatted string.</returns>
        public static string FormatKeyString(string text) =>
            text ?? string.Empty;

        /// <summary>Gets the value of an option.</summary>
        /// <param name="optionName">Name of the option.</param>
        /// <returns>The value.</returns>
        public object GetOption(string optionName) =>
            OptionsList[optionName].value;

        /// <summary>Gets the value of an option.</summary>
        /// <param name="optionName">Name of the option.</param>
        /// <param name="defaultValue">Value to use if option doesn't exist on the list.</param>
        /// <returns>The value.</returns>
        public object GetOption(string optionName, object defaultValue) =>
            OptionsList.TryGetValue(optionName, out var val) ?
            val.value :
            defaultValue;

        /// <summary>Gets the value of an option.</summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="optionName">Name of the option.</param>
        /// <returns>The value.</returns>
        public T GetOption<T>(string optionName) =>
            (T)OptionsList[optionName].value;

        /// <summary>Gets the value of an option.</summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="optionName">Name of the option.</param>
        /// <param name="defaultValue">Value to use if option doesn't exist on the list.</param>
        /// <returns>The value.</returns>
        public T GetOption<T>(string optionName, T defaultValue) =>
            OptionsList.TryGetValue(optionName, out var val) ?
            (T)val.value :
            defaultValue;

        /// <summary>Changes the value of a given option.</summary>
        /// <param name="optionName">Name of the option.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="log">If the change should be logged.</param>
        public void SetOption(string optionName, object value, bool log = true)
        {
            OptionsList.Set(optionName, value);
            OnOptionChanged.Invoke(optionName, new ChangeOptionArgs()
            {
                optionName = optionName,
                value = value,
            });

            if (log)
                Logs.Log($"Changed option '{optionName}' to '{value}'.", "settings_set");
        }

        /// <summary>Changes the value of a given option and applies it.
        /// It's the same as calling <see cref="SetOption(string, object)"/> and <see cref="Apply"/>.</summary>
        /// <param name="optionName">Name of the option.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="log">If the change should be logged.</param>
        public void SetOptionAndApply(string optionName, object value, bool log = true)
        {
            SetOption(optionName, value, log);
            Apply();
        }

        /// <summary>Changes values from a different <see cref="Options.OptionsList">.</summary>
        /// <param name="list">List containing options to set.</param>
        /// <param name="log">If the change should be logged.</param>
        public void SetOptions(OptionsList list, bool log = true)
        {
            foreach (var item in list)
            {
                if (!OptionsList.ContainsKey(item.Key)) continue;
                OptionsList.Set(item.Key, item.Value.value);
                OnOptionChanged.Invoke(item.Key, new ChangeOptionArgs()
                {
                    optionName = item.Key,
                    value = item.Value.value,
                });
            }

            if (log)
                Logs.Log($"Applied options: {string.Join("\n", list.Select(x => $"- {x}"))}", "settings_set_multiple");
        }

        /// <summary>Changes values from a different <see cref="Options.OptionsList"> and applies them
        /// It's the same as calling <see cref="SetOptions"/> and <see cref="Apply"/>.</summary>
        /// <param name="list">List containing options to set.</param>
        /// <param name="log">If the change should be logged.</param>
        public void SetOptionsAndApply(OptionsList list, bool log = true)
        {
            SetOptions(list, log);
            Apply();
        }

        /// <summary>Applies currently set options by saving them.</summary>
        /// <param name="log">If the change should be logged.</param>
        public void Apply(bool log = true)
        {
            try
            {
                Serializer.Save(OptionsList);
            }
            catch (Exception e)
            {
                Logs.LogError($"An exception occured while saving options: {e}");
                return;
            }

            if (log)
                Logs.Log($"Successfully saved options", "settings_save_success");
        }

        /// <summary>Reverts options from the save file.</summary>
        /// <param name="log">If the change should be logged.</param>
        public void Revert(bool log = true)
        {
            try
            {
                var result = Serializer.Load(OptionsList);
                SetOptions(result, log);
            }
            catch (Exception e)
            {
                Logs.LogError($"An exception occured while loading options: {e}");
                return;
            }

            if (log)
                Logs.Log("Successfully loaded options.", "settings_load_success");
        }

        #region Callbacks
        /// <summary>Called whenever an option gets changed.</summary>
        public ActionDictionary<string, ChangeOptionArgs> OnOptionChanged = new ActionDictionary<string, ChangeOptionArgs>();
        #endregion
    }
}