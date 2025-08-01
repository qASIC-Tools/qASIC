using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace qASIC.Logging
{
    [qSkipLogModifiers]
    public class qLogManager
    {
        public qLogManager() : this(qDebug.DEFAULT_TAG, qDebug.WARNING_TAG, qDebug.ERROR_TAG) { }
        public qLogManager(string defaultColorTag) : this(defaultColorTag, qDebug.WARNING_TAG, qDebug.ERROR_TAG) { }
        public qLogManager(string defaultColorTag, string warningColor, string errorColor)
        {
            DefaultColorTag = defaultColorTag;
            WarningColorTag = warningColor;
            ErrorColorTag = errorColor;
        }

        public string DefaultColorTag { get; set; }
        public string WarningColorTag { get; set; }
        public string ErrorColorTag { get; set; }

        public List<qLogModifier> LogModifiers { get; set; }

        #region Closing
        /// <summary>When true, the manager will close itself when all other managers get unregistered.</summary>
        public bool AutoClose { get; set; } = false;
        public bool Closed { get; private set; } = false;
        public Action<qLogManager> OnClose;

        public virtual void Close()
        {
            Closed = true;
            OnClose?.Invoke(this);
        }
        #endregion

        #region Logging
        public event Action<qLog> OnLog;

        protected void InvokeOnLog(qLog log) =>
            OnLog?.Invoke(log);

        public virtual void Log(qLog log)
        {
            if (Closed)
                return;

            ApplyLogModifiers(log);
            InvokeOnLog(log);
        }

        public void Log(string message, string colorTag) =>
            Log(qLog.CreateNow(message, colorTag));

        public void Log(string message, qColor color) =>
            Log(qLog.CreateNow(message, color));

        public void Log(string message) =>
            Log(qLog.CreateNow(message));

        public void LogWarning(string message) =>
            Log(qLog.CreateNow(message, WarningColorTag));

        public void LogError(string message) =>
            Log(qLog.CreateNow(message, ErrorColorTag));
        #endregion

        #region Modifiers
        protected void ApplyLogModifiers(qLog log)
        {
            if (LogModifiers != null)
            {
                Type callingType = null;
                MethodBase callingMethod = null;

                //Find classType and method if any modifier needs them
                if (LogModifiers.Any(x => x.NeedsCallingType))
                {
                    //If this class has already been found in stack trace
                    bool foundThis = false;
                    var stack = new StackTrace();
                    for (int i = 0; i < stack.FrameCount; i++)
                    {
                        var frame = stack.GetFrame(i);
                        var m = frame?.GetMethod();
                        if (m != null)
                        {
                            //We mark this class as found if it's included in the stack trace
                            if (!foundThis &&
                                m.DeclaringType == GetType())
                            {
                                foundThis = true;
                                continue;
                            }

                            //Ignore if class has a skip attribute
                            if (m.DeclaringType?.GetCustomAttribute<qSkipLogModifiersAttribute>() != null)
                                continue;

                            //In case we run into a frame after we already found this class
                            if (foundThis)
                            {
                                //Finish finding
                                callingType = m.DeclaringType;
                                callingMethod = m;
                                break;
                            }
                        }
                    }
                }

                foreach (var item in LogModifiers)
                {
                    item.ModifyLog(log, callingMethod, callingType);
                }
            }
        }
        #endregion

        #region Loggables
        public List<qLogManager> RegisteredManagers { get; private set; } = new List<qLogManager>();

        /// <summary>Subscribes to messages from a <see cref="IHasLogs"/>.</summary>
        /// <param name="loggable">The loggable to register.</param>
        /// <returns>Returns itself.</returns>
        public qLogManager RegisterLoggable(IHasLogs loggable)
        {
            RegisterManager(loggable?.Logs);
            return this;
        }

        /// <summary>Subscribes to messages from a <see cref="qLogManager"/>.</summary>
        /// <param name="other">The other manager to register.</param>
        /// <returns>Returns itself.</returns>
        public virtual qLogManager RegisterManager(qLogManager other)
        {
            if (other != null && other != this && !RegisteredManagers.Contains(other))
            {
                other.OnLog += Log;
                other.OnClose += a => UnregisterManager(a);
                RegisteredManagers.Add(other);
            }

            return this;
        }

        /// <summary>Unsubscribes from messages from a <see cref="IHasLogs"/>.</summary>
        /// <param name="loggable">The loggable to deregister.</param>
        /// <returns>Returns itself.</returns>
        public qLogManager UnregisterLoggable(IHasLogs loggable)
        {
            UnregisterManager(loggable?.Logs);
            return this;
        }

        /// <summary>Unsubscribes from messages from a <see cref="qLogManager"/>.</summary>
        /// <param name="other">The other manager to deregister.</param>
        /// <returns>Returns itself.</returns>
        public virtual qLogManager UnregisterManager(qLogManager other)
        {
            if (other != null && other != this && RegisteredManagers.Contains(other))
            {
                other.OnLog -= Log;
                other.OnClose -= a => UnregisterManager(a);
                RegisteredManagers.Remove(other);

                if (AutoClose && RegisteredManagers.Count > 0)
                    Close();
            }

            return this;
        }
        #endregion
    }
}