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
    /// <summary>
    /// Fluent configuration API for GraphServiceClient with custom handlers and settings
    /// </summary>
    public static class GraphConfiguration
    {
        private static bool _enableChangeTrackingBeforeUpdate = true;

        public static void Reset()
        {
            _enableChangeTrackingBeforeUpdate = true;
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

        #region Fluent API Methods

        /// <summary>
        /// Adds a custom HTTP handler to the Graph client pipeline
        /// </summary>
        /// <param name="handlerFactory">Factory function to create the handler</param>
        /// <returns>A builder instance for method chaining</returns>
        public static GraphConfigurationBuilder AddHttpHandler(Func<DelegatingHandler> handlerFactory)
        {
            ArgumentNullException.ThrowIfNull(handlerFactory);
            return new GraphConfigurationBuilder().AddHttpHandler(handlerFactory);
        }

        /// <summary>
        /// Adds a custom HTTP handler to the Graph client pipeline
        /// </summary>
        /// <typeparam name="T">Type of the handler</typeparam>
        /// <param name="handlerFactory">Factory function to create the handler</param>
        /// <returns>A builder instance for method chaining</returns>
        public static GraphConfigurationBuilder AddHttpHandler<T>(Func<T> handlerFactory) where T : DelegatingHandler
        {
            ArgumentNullException.ThrowIfNull(handlerFactory);
            return new GraphConfigurationBuilder().AddHttpHandler(handlerFactory);
        }

        /// <summary>
        /// Sets the logger to be used by the Graph client and its handlers
        /// </summary>
        /// <param name="logger">The logger instance to use</param>
        /// <returns>A builder instance for method chaining</returns>
        public static GraphConfigurationBuilder WithLogger(ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            return new GraphConfigurationBuilder().WithLogger(logger);
        }

        /// <summary>
        /// Sets the logger factory to be used by the Graph client and its handlers
        /// </summary>
        /// <param name="loggerFactory">The logger factory to use</param>
        /// <returns>A builder instance for method chaining</returns>
        public static GraphConfigurationBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);
            return new GraphConfigurationBuilder().WithLoggerFactory(loggerFactory);
        }

        /// <summary>
        /// Configures whether to remove the default retry handler from the pipeline
        /// </summary>
        /// <param name="remove">True to remove the default retry handler</param>
        /// <returns>A builder instance for method chaining</returns>
        public static GraphConfigurationBuilder SetRemoveDefaultRetryHandler(bool remove = true)
        {
            return new GraphConfigurationBuilder().SetRemoveDefaultRetryHandler(remove);
        }

        /// <summary>
        /// Clears all custom HTTP handlers
        /// </summary>
        /// <returns>A builder instance for method chaining</returns>
        public static GraphConfigurationBuilder ClearHttpHandlers()
        {
            return new GraphConfigurationBuilder().ClearHttpHandlers();
        }

        /// <summary>
        /// Creates a GraphServiceClient with default configuration
        /// </summary>
        /// <param name="credential">Token credential for authentication</param>
        /// <param name="scopes">Scopes for the Graph API</param>
        /// <returns>Configured GraphServiceClient</returns>
        public static GraphServiceClient CreateGraphServiceClient(Azure.Core.TokenCredential credential, string[] scopes)
        {
            return new GraphConfigurationBuilder().CreateGraphServiceClient(credential, scopes);
        }

        #endregion
    }

    /// <summary>
    /// Fluent builder for configuring GraphServiceClient with custom handlers and settings
    /// </summary>
    public class GraphConfigurationBuilder
    {
        private readonly List<Func<ILogger?, DelegatingHandler>> _customHandlerFactories = new();
        private bool _removeDefaultRetryHandler = false;
        private ILogger? _logger;
        private ILoggerFactory? _loggerFactory;

        internal GraphConfigurationBuilder()
        {
        }

        /// <summary>
        /// Adds a custom HTTP handler to the Graph client pipeline
        /// </summary>
        /// <param name="handlerFactory">Factory function to create the handler</param>
        /// <returns>This builder instance for method chaining</returns>
        public GraphConfigurationBuilder AddHttpHandler(Func<DelegatingHandler> handlerFactory)
        {
            ArgumentNullException.ThrowIfNull(handlerFactory);
            _customHandlerFactories.Add(_ => handlerFactory());
            return this;
        }

        /// <summary>
        /// Adds a custom HTTP handler to the Graph client pipeline with logger support
        /// </summary>
        /// <param name="handlerFactory">Factory function to create the handler that takes an optional logger</param>
        /// <returns>This builder instance for method chaining</returns>
        public GraphConfigurationBuilder AddHttpHandler(Func<ILogger?, DelegatingHandler> handlerFactory)
        {
            ArgumentNullException.ThrowIfNull(handlerFactory);
            _customHandlerFactories.Add(handlerFactory);
            return this;
        }

        /// <summary>
        /// Adds a custom HTTP handler to the Graph client pipeline
        /// </summary>
        /// <typeparam name="T">Type of the handler</typeparam>
        /// <param name="handlerFactory">Factory function to create the handler</param>
        /// <returns>This builder instance for method chaining</returns>
        public GraphConfigurationBuilder AddHttpHandler<T>(Func<T> handlerFactory) where T : DelegatingHandler
        {
            ArgumentNullException.ThrowIfNull(handlerFactory);
            _customHandlerFactories.Add(_ => handlerFactory());
            return this;
        }

        /// <summary>
        /// Sets the logger to be used by the Graph client and its handlers
        /// </summary>
        /// <param name="logger">The logger instance to use</param>
        /// <returns>This builder instance for method chaining</returns>
        public GraphConfigurationBuilder WithLogger(ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
            return this;
        }

        /// <summary>
        /// Sets the logger factory to be used by the Graph client and its handlers
        /// </summary>
        /// <param name="loggerFactory">The logger factory to use</param>
        /// <returns>This builder instance for method chaining</returns>
        public GraphConfigurationBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);
            _loggerFactory = loggerFactory;
            return this;
        }

        /// <summary>
        /// Configures whether to remove the default retry handler from the pipeline
        /// </summary>
        /// <param name="remove">True to remove the default retry handler</param>
        /// <returns>This builder instance for method chaining</returns>
        public GraphConfigurationBuilder SetRemoveDefaultRetryHandler(bool remove = true)
        {
            _removeDefaultRetryHandler = remove;
            return this;
        }

        /// <summary>
        /// Clears all custom HTTP handlers
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public GraphConfigurationBuilder ClearHttpHandlers()
        {
            _customHandlerFactories.Clear();
            return this;
        }

        /// <summary>
        /// Creates an HttpClient with the configured custom handlers
        /// </summary>
        /// <returns>Configured HttpClient for use with GraphServiceClient</returns>
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

        /// <summary>
        /// Creates a GraphServiceClient with configured handlers
        /// </summary>
        /// <param name="credential">Token credential for authentication</param>
        /// <param name="scopes">Scopes for the Graph API</param>
        /// <returns>Configured GraphServiceClient</returns>
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