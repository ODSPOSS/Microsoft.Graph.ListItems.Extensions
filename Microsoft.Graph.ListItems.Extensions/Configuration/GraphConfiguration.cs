using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Core;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;
using Microsoft.Extensions.Logging;

namespace Microsoft.Graph.ListItems.Extensions.Configuration
{
    public static class GraphConfiguration
    {
        private static bool _enableChangeTrackingBeforeUpdate = true;
        internal static BatchRetryOptions _defaultBatchRetryOptions = new();

        public static void Reset()
        {
            _enableChangeTrackingBeforeUpdate = true;
            _defaultBatchRetryOptions = new BatchRetryOptions();
        }

        internal static bool ShouldProceedWithUpdate(bool hasChanges)
        {
            if (!_enableChangeTrackingBeforeUpdate)
            {
                return true;
            }

            return hasChanges;
        }

        public static bool IsChangeTrackingEnabled => _enableChangeTrackingBeforeUpdate;

        public static BatchRetryOptions DefaultBatchRetryOptions => _defaultBatchRetryOptions;

        #region Fluent API Methods

        public static GraphConfigurationBuilder AddHttpHandler(Func<DelegatingHandler> handlerFactory)
        {
            ArgumentNullException.ThrowIfNull(handlerFactory);
            return new GraphConfigurationBuilder().AddHttpHandler(handlerFactory);
        }

        public static GraphConfigurationBuilder AddHttpHandler<T>(Func<T> handlerFactory) where T : DelegatingHandler
        {
            ArgumentNullException.ThrowIfNull(handlerFactory);
            return new GraphConfigurationBuilder().AddHttpHandler(handlerFactory);
        }

        public static GraphConfigurationBuilder WithLogger(ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            return new GraphConfigurationBuilder().WithLogger(logger);
        }

        public static GraphConfigurationBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);
            return new GraphConfigurationBuilder().WithLoggerFactory(loggerFactory);
        }

        public static GraphConfigurationBuilder RemoveDefaultRetryHandler()
        {
            return new GraphConfigurationBuilder().RemoveDefaultRetryHandler();
        }

        public static GraphConfigurationBuilder WithBatchRetryOptions(BatchRetryOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return new GraphConfigurationBuilder().WithBatchRetryOptions(options);
        }

        public static GraphConfigurationBuilder WithBatchRetryOptions(Action<BatchRetryOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(configureOptions);
            return new GraphConfigurationBuilder().WithBatchRetryOptions(configureOptions);
        }

        public static GraphConfigurationBuilder ClearHttpHandlers()
        {
            return new GraphConfigurationBuilder().ClearHttpHandlers();
        }

        public static GraphServiceClient CreateGraphServiceClient(Azure.Core.TokenCredential credential, string[] scopes)
        {
            return new GraphConfigurationBuilder().CreateGraphServiceClient(credential, scopes);
        }

        #endregion
    }

    public class BatchRetryOptions
    {
        public int MaxRetries { get; set; } = 5;

        public int InitialDelaySeconds { get; set; } = 1;

        public bool UseExponentialBackoff { get; set; } = true;

        public int MaxDelaySeconds { get; set; } = 60;
    }


    public class GraphConfigurationBuilder
    {
        private readonly List<Func<ILogger?, DelegatingHandler>> _customHandlerFactories = new();
        private bool _removeDefaultRetryHandler = false;
        private ILogger? _logger;
        private ILoggerFactory? _loggerFactory;
        private BatchRetryOptions? _batchRetryOptions;

        internal GraphConfigurationBuilder()
        {
        }

        public GraphConfigurationBuilder AddHttpHandler(Func<DelegatingHandler> handlerFactory)
        {
            ArgumentNullException.ThrowIfNull(handlerFactory);
            _customHandlerFactories.Add(_ => handlerFactory());
            return this;
        }

        public GraphConfigurationBuilder AddHttpHandler(Func<ILogger?, DelegatingHandler> handlerFactory)
        {
            ArgumentNullException.ThrowIfNull(handlerFactory);
            _customHandlerFactories.Add(handlerFactory);
            return this;
        }

        public GraphConfigurationBuilder AddHttpHandler<T>(Func<T> handlerFactory) where T : DelegatingHandler
        {
            ArgumentNullException.ThrowIfNull(handlerFactory);
            _customHandlerFactories.Add(_ => handlerFactory());
            return this;
        }

        public GraphConfigurationBuilder WithLogger(ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
            return this;
        }

        public GraphConfigurationBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);
            _loggerFactory = loggerFactory;
            return this;
        }

        public GraphConfigurationBuilder RemoveDefaultRetryHandler()
        {
            _removeDefaultRetryHandler = true;
            return this;
        }

        public GraphConfigurationBuilder WithBatchRetryOptions(BatchRetryOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _batchRetryOptions = options;
            GraphConfiguration._defaultBatchRetryOptions = options;
            return this;
        }

        public GraphConfigurationBuilder WithBatchRetryOptions(Action<BatchRetryOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(configureOptions);
            _batchRetryOptions = new BatchRetryOptions();
            configureOptions(_batchRetryOptions);
            GraphConfiguration._defaultBatchRetryOptions = _batchRetryOptions;
            return this;
        }

        public GraphConfigurationBuilder ClearHttpHandlers()
        {
            _customHandlerFactories.Clear();
            return this;
        }

        public HttpClient CreateHttpClient()
        {
            IList<DelegatingHandler> handlers = GraphClientFactory.CreateDefaultHandlers();

            // Remove default retry handler if configured
            if (_removeDefaultRetryHandler)
            {
                var defaultRetry = handlers.OfType<RetryHandler>().FirstOrDefault();
                if (defaultRetry != null)
                {
                    handlers.Remove(defaultRetry);
                }
            }

            // Get the logger to pass to handlers
            var loggerToUse = GetLoggerForHandlers();

            // Add custom handlers
            foreach (var handlerFactory in _customHandlerFactories)
            {
                var handler = handlerFactory(loggerToUse);
                handlers.Add(handler);
            }

            return GraphClientFactory.Create(handlers);
        }

        public GraphServiceClient CreateGraphServiceClient(Azure.Core.TokenCredential credential, string[] scopes)
        {
            var httpClient = CreateHttpClient();
            return new GraphServiceClient(httpClient, credential, scopes);
        }

        private ILogger? GetLoggerForHandlers()
        {
            if (_logger != null)
            {
                return _logger;
            }
            
            if (_loggerFactory != null)
            {
                return _loggerFactory.CreateLogger("Microsoft.Graph.Extensions");
            }

            return null;
        }
    }
}