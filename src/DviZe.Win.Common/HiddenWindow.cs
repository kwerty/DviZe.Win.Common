using Kwerty.DviZe.Workers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Win;

public sealed class HiddenWindow : IAsyncDisposable
{
    readonly RunSingle<HiddenWindowSession> sessionRunner;
    readonly Runner<HiddenWindowSubscription> subscriptionRunner;
    readonly HiddenWindowOptions options;
    readonly IThreadAccessor threadAccessor;
    readonly ILoggerFactory loggerFactory;

    public HiddenWindow(HiddenWindowOptions options, IThreadAccessor threadAccessor, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(threadAccessor, nameof(threadAccessor));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));

        this.options = options;
        this.threadAccessor = threadAccessor;
        this.loggerFactory = loggerFactory;
        sessionRunner = new(loggerFactory);
        subscriptionRunner = new(loggerFactory);
    }

    public IntPtr? Hwnd
    {
        get
        {
            _ = sessionRunner.Provider.TryGet(out var session);
            return session?.hwnd;
        }
    }

    public async Task InstallAsync(CancellationToken cancellationToken = default)
    {
        var session = new HiddenWindowSession(options, threadAccessor, loggerFactory);
        await sessionRunner.StartWorkerAsync(session, cancellationToken).ConfigureAwait(false);
    }

    public Task<IDisposable> SubscribeAsync(Action<HiddenWindowEvent> callback, CancellationToken cancellationToken = default)
        => SubscribeAsyncCore(msg: null, callback, cancellationToken);

    public Task<IDisposable> SubscribeAsync(uint msg, Action<HiddenWindowEvent> callback, CancellationToken cancellationToken = default)
        => SubscribeAsyncCore(msg, callback, cancellationToken);

    async Task<IDisposable> SubscribeAsyncCore(uint? msg, Action<HiddenWindowEvent> callback, CancellationToken cancellationToken)
    {
        var subscription = new HiddenWindowSubscription(msg, callback, sessionRunner, threadAccessor);
        await subscriptionRunner.StartWorkerAsync(subscription, cancellationToken).ConfigureAwait(false);
        return subscription;
    }

    public async ValueTask DisposeAsync()
    {
        await subscriptionRunner.DisposeAsync().ConfigureAwait(false);
        await sessionRunner.DisposeAsync().ConfigureAwait(false);
    }
}
