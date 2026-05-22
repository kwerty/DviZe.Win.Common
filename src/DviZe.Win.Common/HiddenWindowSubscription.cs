using Kwerty.DviZe.Workers;
using System;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Win;

internal sealed class HiddenWindowSubscription(uint? msg, Action<HiddenWindowEvent> callback, IWorkerProvider<HiddenWindowSession> sessionProvider, IThreadAccessor threadAccessor)
    : Worker, IDisposable
{
    HiddenWindowSession session;
    IDisposable sessionReleaser;
    uint handlerRegistrationId;

    protected override async Task OnStartingAsync(WorkerStartingContext startingContext)
    {
        (session, sessionReleaser) = await sessionProvider.LeaseAsync(startingContext.CancellationToken).ConfigureAwait(false);

        await threadAccessor.UIThread;

        handlerRegistrationId = HiddenWindowNativeExtensions.RegisterHandler(session.hwnd, msg, callback);
    }

    protected override async Task OnStoppingAsync()
    {
        await threadAccessor.UIThread;

        HiddenWindowNativeExtensions.UnregisterHandler(handlerRegistrationId);

        sessionReleaser.Dispose();
    }

    void IDisposable.Dispose() => Context.TryStop();
}
