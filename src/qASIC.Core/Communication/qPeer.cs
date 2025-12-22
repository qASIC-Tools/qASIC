using qASIC.Communication.Components;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using qASIC.Logging;

namespace qASIC.Communication;

public abstract class qPeer : IPeer, IHasLogs
{
    public CommsComponentCollection Components { get; protected set; }

    public qLogManager Logs { get; } = new qLogManager();

    public virtual bool IsActive { get; protected set; } = false;

    private readonly qPriorityQueue<KeyValuePair<Action, long>, long> eventQueue = new();

    public int MilisecondsPerUpdate { get; set; }
    public int MilisecondsPerSend { get; set; } = 10;

    private readonly System.Diagnostics.Stopwatch stopwatch = new();
    private long CurrentTime { get; set; }

    CancellationTokenSource updateCancel;

    public void StartUpdateLoop(int milisecondsPerUpdate = 10)
    {
        StopUpdateLog();

        MilisecondsPerUpdate = milisecondsPerUpdate;

        updateCancel = new CancellationTokenSource();
        Task.Run(async () =>
        {
            var cancel = updateCancel;
            while (cancel != null && !cancel.IsCancellationRequested && MilisecondsPerUpdate > 0)
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

        OnUpdate();
    }

    public virtual void OnUpdate() { }

    public void StopUpdateLog()
    {
        updateCancel?.Cancel();
        updateCancel = null;
    }

    protected void PrepareStart()
    {
        CurrentTime = 0;
        stopwatch.Restart();
    }

    protected void PrepareStop()
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
