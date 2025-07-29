using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Core;
using Microsoft.Graph.ListItems.Extensions.Configuration;

namespace Microsoft.Graph.ListItems.Extensions.Extensions
{
    public static class GraphBatchExtensions
    {
        public static async Task<(IReadOnlyDictionary<string, HttpStatusCode> Statuses, Dictionary<string, HttpResponseMessage> BatchResponse)> PostBatchWithFailedDependencyRetriesAsync(
            this GraphServiceClient graphClient, 
            BatchRequestContentCollection originalBatch, 
            int? maxRetries = null, 
            int? initialDelaySeconds = null,
            ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(graphClient);
            ArgumentNullException.ThrowIfNull(originalBatch);

            var options = GraphConfiguration.DefaultBatchRetryOptions;
            var actualMaxRetries = maxRetries ?? options.MaxRetries;
            var actualInitialDelay = initialDelaySeconds ?? options.InitialDelaySeconds;

            if (actualMaxRetries < 1)
                throw new ArgumentException("Maximum retries must be at least 1", nameof(maxRetries));

            if (actualInitialDelay < 1)
                throw new ArgumentException("Initial delay must be at least 1 second", nameof(initialDelaySeconds));

            TimeSpan delay = TimeSpan.FromSeconds(actualInitialDelay);
            var keyValuePairs = new Dictionary<string, HttpResponseMessage>();
            var results = new Dictionary<string, HttpStatusCode>();
            var batchToSend = originalBatch;

            logger?.LogInformation("Starting batch request with retry logic. Max retries: {MaxRetries}, Initial delay: {InitialDelay}s", 
                actualMaxRetries, actualInitialDelay);

            for (int attempt = 1; attempt <= actualMaxRetries; attempt++)
            {
                logger?.LogDebug("Executing batch request attempt {Attempt}/{MaxRetries}", attempt, actualMaxRetries);

                try
                {
                    BatchResponseContentCollection batchResponse = await graphClient.Batch.PostAsync(batchToSend);
                    Dictionary<string, HttpStatusCode> responses = await batchResponse.GetResponsesStatusCodesAsync();

                    // Find failed dependencies (non-success status codes, excluding redirects and non-retryable errors)
                    Dictionary<string, HttpStatusCode> failedDeps = responses
                        .Where(kvp => !BatchResponseContent.IsSuccessStatusCode(kvp.Value) && 
                                      ShouldRetryBatchRequest(kvp.Value))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    // Log any non-retryable errors for visibility
                    var nonRetryableErrors = responses
                        .Where(kvp => !BatchResponseContent.IsSuccessStatusCode(kvp.Value) && 
                                     !ShouldRetryBatchRequest(kvp.Value))
                        .ToList();

                    if (nonRetryableErrors.Any())
                    {
                        foreach (var error in nonRetryableErrors)
                        {
                            logger?.LogWarning("Request {RequestId} failed with non-retryable status code {StatusCode} and will not be retried", 
                                error.Key, error.Value);
                        }
                    }

                    logger?.LogDebug("Batch attempt {Attempt} completed. Failed requests: {FailedCount}/{TotalCount}", 
                        attempt, failedDeps.Count, responses.Count);

                    // If no failures or we've reached max retries, collect all responses and exit
                    if (failedDeps.Count == 0 || attempt == actualMaxRetries)
                    {
                        foreach (var kvp in responses)
                        {
                            var res = await batchResponse.GetResponseByIdAsync(kvp.Key);
                            keyValuePairs[kvp.Key] = res;
                            results[kvp.Key] = kvp.Value;
                        }

                        if (failedDeps.Count == 0)
                        {
                            logger?.LogInformation("Batch request completed successfully on attempt {Attempt}", attempt);
                        }
                        else
                        {
                            logger?.LogWarning("Batch request completed with {FailedCount} failures after {MaxRetries} attempts", 
                                failedDeps.Count, actualMaxRetries);
                        }
                        break;
                    }

                    // Calculate delay with exponential backoff if enabled
                    var nextDelay = CalculateDelay(delay, options, attempt);
                    
                    logger?.LogInformation("Waiting {DelaySeconds} seconds before retry attempt {NextAttempt}...", 
                        nextDelay.TotalSeconds, attempt + 1);
                    await Task.Delay(nextDelay);
                    delay = nextDelay;

                    // Prepare batch for retry with only failed requests that should be retried
                    var responsesForRetry = responses
                        .Where(x => !BatchResponseContent.IsSuccessStatusCode(x.Value) && 
                                   ShouldRetryBatchRequest(x.Value))
                        .ToDictionary();

                    batchToSend = CreateBatchWithFailedRequests(graphClient, originalBatch, responsesForRetry);

                    // Preserve original request IDs by mapping them back
                    var stepsSnapshot = batchToSend.BatchRequestSteps.ToArray();

                    foreach (var kv in stepsSnapshot)
                    {
                        var oldStepId = kv.Key;
                        var step = kv.Value;
                        var requestPath = step.Request.RequestUri!.AbsolutePath;

                        // Find the matching original request
                        var matchingOriginal = originalBatch.BatchRequestSteps
                            .First(x => x.Value.Request.RequestUri!.AbsolutePath == requestPath);

                        var newStepId = matchingOriginal.Key;

                        // Replace the step with the original ID
                        batchToSend.RemoveBatchRequestStepWithId(oldStepId);

                        var newStep = new BatchRequestStep(
                            requestId: newStepId,
                            httpRequestMessage: step.Request,
                            dependsOn: step.DependsOn);

                        batchToSend.AddBatchRequestStep(newStep);
                    }

                    logger?.LogDebug("Prepared retry batch with {RetryCount} requests", batchToSend.BatchRequestSteps.Count);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error occurred during batch request attempt {Attempt}", attempt);
                    
                    if (attempt == actualMaxRetries)
                    {
                        throw; // Re-throw on final attempt
                    }

                    // Wait before retry on exception with exponential backoff
                    var nextDelay = CalculateDelay(delay, options, attempt);
                    await Task.Delay(nextDelay);
                    delay = nextDelay;
                }
            }

            return (results, keyValuePairs);
        }

        public static async Task<(IReadOnlyDictionary<string, HttpStatusCode> Statuses, Dictionary<string, HttpResponseMessage> BatchResponse)> PostBatchWithRetriesAsync(
            this GraphServiceClient graphClient, 
            BatchRequestContentCollection originalBatch,
            ILogger? logger = null)
        {
            return await PostBatchWithFailedDependencyRetriesAsync(graphClient, originalBatch, null, null, logger);
        }

        public static async Task<(IReadOnlyDictionary<string, HttpStatusCode> Statuses, Dictionary<string, HttpResponseMessage> BatchResponse)> PostBatchWithRetriesAsync(
            this GraphServiceClient graphClient, 
            BatchRequestContentCollection originalBatch,
            BatchRetryOptions options,
            ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(options);
            return await PostBatchWithFailedDependencyRetriesAsync(graphClient, originalBatch, options.MaxRetries, options.InitialDelaySeconds, logger);
        }

        private static TimeSpan CalculateDelay(TimeSpan currentDelay, BatchRetryOptions options, int attempt)
        {
            if (!options.UseExponentialBackoff)
            {
                return currentDelay;
            }

            var nextDelaySeconds = Math.Min(currentDelay.TotalSeconds * 2, options.MaxDelaySeconds);
            return TimeSpan.FromSeconds(nextDelaySeconds);
        }

        private static bool ShouldRetryBatchRequest(HttpStatusCode statusCode)
        {
            // Don't retry on client errors that indicate permanent issues
            if (statusCode == HttpStatusCode.NotFound ||           // 404 - Resource not found
                statusCode == HttpStatusCode.Unauthorized ||       // 401 - Authentication required
                statusCode == HttpStatusCode.Forbidden ||          // 403 - Access denied
                statusCode == HttpStatusCode.BadRequest ||         // 400 - Bad request format
                statusCode == HttpStatusCode.MethodNotAllowed ||   // 405 - Method not allowed
                statusCode == HttpStatusCode.Conflict ||           // 409 - Resource conflict
                statusCode == HttpStatusCode.Gone ||               // 410 - Resource permanently removed
                statusCode == HttpStatusCode.Found)                // 302 - Redirect (already handled)
            {
                return false;
            }

            // Retry on server errors and throttling
            return statusCode == HttpStatusCode.ServiceUnavailable ||     // 503 - Service unavailable
                   statusCode == HttpStatusCode.GatewayTimeout ||         // 504 - Gateway timeout
                   statusCode == HttpStatusCode.InternalServerError ||    // 500 - Internal server error
                   statusCode == HttpStatusCode.BadGateway ||             // 502 - Bad gateway
                   statusCode == (HttpStatusCode)429 ||                   // 429 - Too many requests
                   statusCode == (HttpStatusCode)423;                     // 423 - Locked (SharePoint specific)
        }

        private static BatchRequestContentCollection CreateBatchWithFailedRequests(
            GraphServiceClient graphClient,
            BatchRequestContentCollection originalBatch,
            Dictionary<string, HttpStatusCode> failedResponses)
        {
            ArgumentNullException.ThrowIfNull(graphClient);
            ArgumentNullException.ThrowIfNull(originalBatch);
            ArgumentNullException.ThrowIfNull(failedResponses);

            var newBatch = new BatchRequestContentCollection(graphClient);

            // Add only the failed request steps to the new batch
            foreach (var failedResponseId in failedResponses.Keys)
            {
                if (originalBatch.BatchRequestSteps.TryGetValue(failedResponseId, out var originalStep))
                {
                    // Create a new batch step based on the original
                    var newStep = new BatchRequestStep(
                        requestId: originalStep.RequestId,
                        httpRequestMessage: originalStep.Request,
                        dependsOn: originalStep.DependsOn);

                    newBatch.AddBatchRequestStep(newStep);
                }
            }

            return newBatch;
        }
    }
}