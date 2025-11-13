using MediatR;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Logging
{
    public class ProcessingLogBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ProcessingLogBehavior(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var timestampUtc = DateTime.UtcNow;

            try
            {
                var response = await next();

                bool success = true;
                string message = $"Request {requestName} completed successfully.";
                int? responseCode = null;

                if (response is BaseResponse baseResponse)
                {
                    success = baseResponse.Success;

                    if (!string.IsNullOrWhiteSpace(baseResponse.Message))
                    {
                        message = baseResponse.Message;
                    }
                    responseCode = baseResponse.ResponseCode;
                }

                await WriteLogAsync(new ProcessingLog
                {
                    TimestampUtc = timestampUtc,
                    Level = success ? "Info" : "Warning",
                    RequestName = requestName,
                    Success = success,
                    Message = message,
                    ResponseCode = responseCode
                }, cancellationToken);

                return response;
            }
            catch (Exception ex)
            {
                await WriteLogAsync(new ProcessingLog
                {
                    TimestampUtc = timestampUtc,
                    Level = "Error",
                    RequestName = requestName,
                    Success = false,
                    Message = $"Request {requestName} failed.",
                    ExceptionType = ex.GetType().FullName,
                    ExceptionMessage = ex.Message,
                    ExceptionStackTrace = ex.StackTrace
                }, cancellationToken);

                throw;
            }
        }

        private async Task WriteLogAsync(ProcessingLog log, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StargateContext>();

            await context.ProcessingLogs.AddAsync(log, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

    }
}