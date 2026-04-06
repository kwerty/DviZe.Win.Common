using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Kwerty.DviZe.Win;

public interface IThreadAccessor
{
    public ThreadPoolAwaitable ThreadPool { get; }

    public UIThreadAwaitable UIThread { get; }

    public readonly struct ThreadPoolAwaitable(MessagePump messagePump)
    {
        public readonly ThreadPoolAwaiter GetAwaiter() => new(messagePump);

        public readonly struct ThreadPoolAwaiter(MessagePump messagePump) : INotifyCompletion, ICriticalNotifyCompletion
        {
            public readonly bool IsCompleted => Environment.CurrentManagedThreadId != messagePump.uiThreadId;

            public readonly void OnCompleted(Action continuation)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(static c => c(), continuation, preferLocal: false);
            }

            public readonly void UnsafeOnCompleted(Action continuation)
            {
                System.Threading.ThreadPool.UnsafeQueueUserWorkItem(static c => c(), continuation, preferLocal: false);
            }

            public readonly void GetResult() { }
        }
    }

    public readonly struct UIThreadAwaitable(MessagePump messagePump)
    {
        public readonly UIThreadAwaiter GetAwaiter() => new(messagePump);

        public readonly struct UIThreadAwaiter(MessagePump messagePump) : INotifyCompletion, ICriticalNotifyCompletion
        {
            public readonly bool IsCompleted => Environment.CurrentManagedThreadId == messagePump.uiThreadId;

            public readonly void OnCompleted(Action continuation)
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
            }

            public readonly void UnsafeOnCompleted(Action continuation)
            {
                messagePump.Post(static c => ((Action)c)(), continuation);
            }

            public readonly void GetResult() { }
        }
    }
}
