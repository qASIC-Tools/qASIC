using qASIC.Communication.Components;
using qASIC.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace qASIC.Communication
{
    public abstract class qPeer : IPeer, IHasLogs
    {
        public CommsComponentCollection Components { get; protected set; }

        public LogManager Logs { get; set; } = new LogManager();

        public virtual bool IsActive { get; protected set; } = false;

        qPriorityQueue<KeyValuePair<Action, long>, long> eventQueue = new qPriorityQueue<KeyValuePair<Action, long>, long>();

        public int MilisecondsPerUpdate { get; set; }
        public int MilisecondsPerSend { get; set; } = 50;

        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private long CurrentTime { get; set; }

        CancellationTokenSource updateCancel;

        public void StartUpdateLoop(int milisecondsPerUpdate = 50)
        {
            StopUpdateLog();

            MilisecondsPerUpdate = milisecondsPerUpdate;

            updateCancel = new CancellationTokenSource();
            Task.Run(async () =>
            {
                var cancel = updateCancel;
                while (!cancel.IsCancellationRequested && MilisecondsPerUpdate > 0)
                {
                    Update();
                    await Task.Delay(MilisecondsPerUpdate);
                }
            });
        }

        public void Update()
        {
            if (!IsActive) return;

            CurrentTime = stopwatch.ElapsedMilliseconds;

            while (eventQueue.Count > 0 && eventQueue.Peek().Value <= CurrentTime)
            {
                try
                {
                    eventQueue.Dequeue().Key.Invoke();
                }
                catch (Exception e)
                {
                    Logs.LogError($"There was a problem in update loop, {e}");
                }
            }
        }

        public void StopUpdateLog()
        {
            if (updateCancel != null)
            {
                updateCancel.Cancel();
                updateCancel = null;
            }
        }

        protected void OnStart()
        {
            CurrentTime = 0;
            stopwatch.Restart();
        }

        protected void OnStop()
        {
            StopUpdateLog();
            eventQueue.Clear();
            CurrentTime = 0;
            stopwatch.Stop();
        }

        public abstract void Send(qPacket packet);

        protected void ExecuteLater(long inMs, Action delayedAction)
        {
            var t = CurrentTime + inMs;
            eventQueue.Enqueue(new KeyValuePair<Action, long>(delayedAction, t), t);
        }
    }
}