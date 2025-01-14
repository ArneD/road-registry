namespace RoadRegistry.AdminHost.Consumers;

using BackOffice;
using BackOffice.Abstractions;
using BackOffice.Configuration;
using Be.Vlaanderen.Basisregisters.MessageHandling.AwsSqs.Simple;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class AdminMessageConsumer
{
    private readonly SqsQueueUrlOptions _sqsQueueUrlOptions;
    private readonly ISqsQueueConsumer _sqsConsumer;
    private readonly SqsOptions _sqsOptions;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;

    public AdminMessageConsumer(
        IMediator mediator,
        ILogger<AdminMessageConsumer> logger,
        SqsQueueUrlOptions sqsQueueUrlOptions,
        ISqsQueueConsumer sqsQueueConsumer,
        SqsOptions sqsOptions
     )
    {
        _mediator = mediator;
        _sqsQueueUrlOptions = sqsQueueUrlOptions;
        _sqsConsumer = sqsQueueConsumer;
        _sqsOptions = sqsOptions;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var lastMessage = await _sqsConsumer.Consume(_sqsQueueUrlOptions.Admin, async message =>
            {
                var sqsMessageType = message.GetType();
                _logger.LogInformation("SQS message '{Type}' received", sqsMessageType.FullName);

                var backOfficeRequest = GetBackOfficeRequestFromSqsRequest(message);
                if (backOfficeRequest is not null)
                {
                    _logger.LogInformation("Continuing with BackOffice request of type '{Type}'", backOfficeRequest.GetType().FullName);
                }

                var result = await _mediator.Send(backOfficeRequest ?? message, stoppingToken);
                _logger.LogInformation("SQS message result: {Result}", JsonConvert.SerializeObject(result, _sqsOptions.JsonSerializerSettings));
            }, stoppingToken);

            if (lastMessage?.Error is not null)
            {
                _logger.LogError("SQS message processing failed: {Error}", lastMessage.Error);
            }
            else if (string.IsNullOrEmpty(lastMessage?.Message?.Type))
            {
                _logger.LogInformation("No SQS message received");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"An unhandled exception has occurred: {ex.Message}");
        }
    }

    private static object GetBackOfficeRequestFromSqsRequest(object sqsMessage)
    {
        var backOfficeRequestType = sqsMessage.GetType()
            .GetInterfaces()
            .Where(iType => iType.IsGenericType && iType.GetGenericTypeDefinition() == typeof(IHasBackOfficeRequest<>))
            .Select(iType => iType.GetGenericArguments()[0])
            .FirstOrDefault();
        
        if (backOfficeRequestType is not null)
        {
            var requestProperty = sqsMessage.GetType().GetProperty(nameof(IHasBackOfficeRequest<object>.Request));
            return requestProperty!.GetValue(sqsMessage);
        }

        return null;
    }
}
