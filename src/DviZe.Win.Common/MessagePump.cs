using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Kwerty.DviZe.Win;

public sealed class MessagePump : SynchronizationContext, IThreadAccessor, IDisposable
{
    static MessagePump instance;
    readonly BlockingCollection<(SendOrPostCallback Callback, object State)> queue = [];
    readonly IThreadAccessor.ThreadPoolAwaitable threadPoolAwaitable;
    readonly IThreadAccessor.UIThreadAwaitable uiThreadAwaitable;
    readonly internal int uiThreadId;
    volatile int state;

    MessagePump()
    {
        threadPoolAwaitable = new IThreadAccessor.ThreadPoolAwaitable(this);
        uiThreadAwaitable = new IThreadAccessor.UIThreadAwaitable(this);
        uiThreadId = Environment.CurrentManagedThreadId;
        MessagePumpNativeExtensions.SetUIThread();
    }

    public IThreadAccessor ThreadAccessor => this;

    IThreadAccessor.ThreadPoolAwaitable IThreadAccessor.ThreadPool => threadPoolAwaitable;

    IThreadAccessor.UIThreadAwaitable IThreadAccessor.UIThread => uiThreadAwaitable;

    void Run()
    {
        using (queue)
        {
            while (true)
            {
                if (queue.TryTake(out var item))
                {
                    item.Callback(item.State);
                }
                else
                {
                    if (state == State.Stopped)
                    {
                        break;
                    }

                    if (!MessagePumpNativeExtensions.Run())
                    {
                        break; // Shouldn't happen, unless user does something weird (eg.. calls PostQuitMessage).
                    }
                }
            }

            // Continue executing posted items until Dispose completes the queue.
            while (queue.TryTake(out var item, Timeout.Infinite))
            {
                item.Callback(item.State);
            }
        }
    }

    /// <summary>
    /// Dispatches a synchronous message to the synchronization context.
    /// </summary>
    /// <remarks>
    /// <para>Calling this method directly is discouraged. Prefer <see cref="IThreadAccessor"/> for thread switching.</para>
    /// <para><paramref name="sendOrPostCallback" /> must not throw.</para>
    /// </remarks>
    public override void Send(SendOrPostCallback sendOrPostCallback, object state)
    {
        if (Environment.CurrentManagedThreadId == uiThreadId)
        {
            sendOrPostCallback(state);
        }
        else
        {
            using var waitHandle = new ManualResetEventSlim();

            Post(_ =>
            {
                sendOrPostCallback(state);
                waitHandle.Set();
            }, state: null);

            waitHandle.Wait();
        }
    }

    /// <summary>
    /// Dispatches an asynchronous message to the synchronization context.
    /// </summary>
    /// <remarks>
    /// <para>Calling this method directly is discouraged. Prefer <see cref="IThreadAccessor"/> for thread switching.</para>
    /// <para><paramref name="sendOrPostCallback" /> must not throw.</para>
    /// </remarks>
    public override void Post(SendOrPostCallback sendOrPostCallback, object state)
    {
        queue.Add((sendOrPostCallback, state));

        if (this.state == State.Running)
        {
            MessagePumpNativeExtensions.Wake();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref state, State.Stopped) == State.Running)
        {
            queue.CompleteAdding();

            MessagePumpNativeExtensions.Wake();

            instance = null;
        }
    }

    public static InstallAwaitable Install()
    {
        if (Interlocked.CompareExchange(ref instance, new MessagePump(), null) != null)
        {
            throw new InvalidOperationException();
        }

        return new InstallAwaitable(instance);
    }

    public readonly struct InstallAwaitable(MessagePump messagePump)
    {
        public readonly InstallAwaiter GetAwaiter() => new(messagePump);

        public readonly struct InstallAwaiter(MessagePump messagePump) : INotifyCompletion
        {
            public readonly bool IsCompleted => messagePump.state == State.Running;

            public void OnCompleted(Action continuation)
            {
                var executionContext = ExecutionContext.Capture();
                messagePump.Post(_ =>
                {
                    if (executionContext != null)
                    {
                        using (executionContext)
                        {
                            ExecutionContext.Run(executionContext, _ => continuation(), state: null);
                        }
                    }
                    else
                    {
                        continuation();
                    }
                }, state: null);

                if (Interlocked.CompareExchange(ref messagePump.state, State.Running, State.NotActive) == State.NotActive)
                {
                    messagePump.Run();
                }
            }

            public readonly MessagePump GetResult()
            {
                return messagePump.state == State.Running
                    ? messagePump
                    : throw new InvalidOperationException();
            }
        }
    }

    static class State
    {
        public const int NotActive = 0;
        public const int Running = 1;
        public const int Stopped = 2;
    }
}