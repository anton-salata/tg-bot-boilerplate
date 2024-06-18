using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TgBotBoileplate.DAL.Models;
using TgBotBoileplate.DAL.Repositories;

namespace TgBotBoileplate.Service.Logging
{
    public class HttpLoggingHandler : DelegatingHandler
    {
        private readonly ILogger<HttpLoggingHandler> _logger;
        private readonly IRepository<HttpLogRecord> _trackingRepository;
        private readonly string _httpClientName;

        public HttpLoggingHandler(ILogger<HttpLoggingHandler> logger, IRepository<HttpLogRecord> trackingRepository, string httpClientName)
        {
            _logger = logger ?? new NullLogger<HttpLoggingHandler>();
            _trackingRepository = trackingRepository ?? throw new ArgumentNullException(nameof(trackingRepository));
            _httpClientName = httpClientName ?? string.Empty;

        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Capture the current UTC time for logging
            var actionDateTime = DateTime.UtcNow;

            // Log the request
            _logger.LogInformation($"{_httpClientName} Request: {request.Method} {request.RequestUri}");

            // Log request headers
            foreach (var header in request.Headers)
            {
                _logger.LogInformation($"{_httpClientName} Request Header: {header.Key}: {string.Join(",", header.Value)}");
            }

            string? requestBody = null;
            if (request.Content != null)
            {
                // Log request content headers
                foreach (var header in request.Content.Headers)
                {
                    _logger.LogInformation($"{_httpClientName} Request Content Header: {header.Key}: {string.Join(",", header.Value)}");
                }

                requestBody = await request.Content.ReadAsStringAsync();
                _logger.LogInformation($"{_httpClientName} Request Body: {requestBody}");
            }

            // Send the request and get the response
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            // Log response headers
            foreach (var header in response.Headers)
            {
                _logger.LogInformation($"{_httpClientName} Response Header: {header.Key}: {string.Join(",", header.Value)}");
            }

            // Log the response
            _logger.LogInformation($"{_httpClientName} Response: {(int)response.StatusCode} {response.ReasonPhrase}");
            string? responseBody = null;
            if (response.Content != null)
            {
                // Log response content headers
                foreach (var header in response.Content.Headers)
                {
                    _logger.LogInformation($"{_httpClientName} Response Content Header: {header.Key}: {string.Join(",", header.Value)}");
                }

                responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"{_httpClientName} Response Body: {responseBody}");
            }

            await LogToDatabase(request, requestBody, response, responseBody, actionDateTime);

            return response;
        }

        protected async Task LogToDatabase(HttpRequestMessage request, string? requestBody, HttpResponseMessage response, string? responseBody, DateTime actionDateTime)
        {
            var logRecord = new HttpLogRecord
            {
                Uri = request.RequestUri?.AbsoluteUri,
                Method = request.Method.Method,
                RequestHeaders = request.Headers.ToDictionary(h => h.Key, h => h.Value),
                RequestBody = requestBody,
                ActionDateTime = actionDateTime,
                StatusCode = ((int)response.StatusCode).ToString(),
                ReasonPhrase = response.ReasonPhrase,
                ResponseHeaders = response.Headers.ToDictionary(h => h.Key, h => h.Value),
                ResponseBody = responseBody,
                ClientName = _httpClientName
            };

            // Save the log to the database
            await _trackingRepository.Insert(logRecord);
        }
    }
}
