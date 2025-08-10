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
        public qLogManager(string defaultTag, string warningTag, string errorTag)
        {
            DefaultTag = defaultTag;
            WarningTag = warningTag;
            ErrorTag = errorTag;
        }

        public string DefaultTag { get; set; }
        public string WarningTag { get; set; }
        public string ErrorTag { get; set; }

        #region Closing
        /// <summary>When true, the manager will close itself when all other managers get unregistered.</summary>
        public bool AutoClose { get; set; } = false;
        public bool Closed { get; private set; } = false;
        public Action<qLogManager> OnClose;

        public void StartClosing()
        {
            if (RegisteredManagers.Count > 0)
            {
                AutoClose = true;
                return;
            }

            ForceClose();
        }

        public virtual void ForceClose()
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
            Log(qLog.CreateNow(message, WarningTag));

        public void LogError(string message) =>
            Log(qLog.CreateNow(message, ErrorTag));
        #endregion

        #region Modifiers
        public List<qLogModifier> LogModifiers { get; set; }

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

        #region Registering
        protected List<qLogManager> RegisteredManagers { get; private set; } = new List<qLogManager>();

        /// <summary>Subscribes to messages from a <see cref="IHasLogs"/>.</summary>
        /// <param name="loggable">The loggable to register.</param>
        /// <returns>Returns itself.</returns>
        public qLogManager Register(IHasLogs loggable)
        {
            Register(loggable?.Logs);
            return this;
        }

        /// <summary>Subscribes to messages from a <see cref="qLogManager"/>.</summary>
        /// <param name="other">The other manager to register.</param>
        /// <returns>Returns itself.</returns>
        public virtual qLogManager Register(qLogManager other)
        {
            if (other != null && other != this && !RegisteredManagers.Contains(other))
            {
                other.OnLog += Log;
                other.OnClose += a => Unregister(a);
                RegisteredManagers.Add(other);
            }

            return this;
        }

        /// <summary>Unsubscribes from messages from a <see cref="IHasLogs"/>.</summary>
        /// <param name="loggable">The loggable to deregister.</param>
        /// <returns>Returns itself.</returns>
        public qLogManager Unregister(IHasLogs loggable)
        {
            Unregister(loggable?.Logs);
            return this;
        }

        /// <summary>Unsubscribes from messages from a <see cref="qLogManager"/>.</summary>
        /// <param name="other">The other manager to deregister.</param>
        /// <returns>Returns itself.</returns>
        public virtual qLogManager Unregister(qLogManager other)
        {
            if (other != null && other != this && RegisteredManagers.Contains(other))
            {
                other.OnLog -= Log;
                other.OnClose -= a => Unregister(a);
                RegisteredManagers.Remove(other);

                if (AutoClose && RegisteredManagers.Count > 0)
                    ForceClose();
            }

            return this;
        }
        #endregion
    }
}