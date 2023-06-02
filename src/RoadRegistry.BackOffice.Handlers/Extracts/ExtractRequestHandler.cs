namespace RoadRegistry.BackOffice.Handlers.Extracts;

using Abstractions;
using Editor.Schema;
using Editor.Schema.Extracts;
using Framework;
using Microsoft.Extensions.Logging;

public abstract class ExtractRequestHandler<TRequest, TResponse> : EndpointRequestHandler<TRequest, TResponse>
    where TRequest : EndpointRequest<TResponse>
    where TResponse : EndpointResponse
{
    protected readonly EditorContext _context;

    protected ExtractRequestHandler(
        EditorContext context,
        CommandHandlerDispatcher dispatcher,
        ILogger logger) : base(dispatcher, logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        var downloadId = new DownloadId(Guid.NewGuid());
        var randomExternalRequestId = Guid.NewGuid().ToString("N");

        return await HandleRequestAsync(request, downloadId, randomExternalRequestId, cancellationToken);
    }

    public abstract Task<TResponse> HandleRequestAsync(TRequest request, DownloadId downloadId, string randomExternalRequestId, CancellationToken cancellationToken);
}
