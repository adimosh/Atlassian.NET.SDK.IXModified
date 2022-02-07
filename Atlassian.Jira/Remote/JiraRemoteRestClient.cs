using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IX.Remote.Envelopes;
using IX.StandardExtensions;
using IX.StandardExtensions.Contracts;
using IX.StandardExtensions.RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Atlassian.Jira.Remote
{
    internal class JiraRemoteRestClient : IJiraRestClient
    {
        private readonly string _url;
        private JiraRestClientSettings _settings;
        private readonly RestClient _remoteRestClient;

        public JiraRemoteRestClient(string url, JiraRestClientSettings settings)
        {
            Requires.NotNullOrWhiteSpace(
                out _url,
                url,
                nameof(url));
            Requires.NotNull(
                out _settings,
                settings,
                nameof(settings));

            this._remoteRestClient = new RestClient(
                new RestClientOptions(_url)
                {
                    Proxy = _settings.Proxy,
                });
        }

        /// <summary>
        /// Base url of the Jira server.
        /// </summary>
        public string Url => this._url;

        /// <summary>
        /// Settings to configure the rest client.
        /// </summary>
        public JiraRestClientSettings Settings => this._settings;

        /// <summary>
        /// Executes a request.
        /// </summary>
        /// <param name="request">Request object.</param>
        /// <param name="token">Cancellation token for the operation.</param>
        public async Task<RestResponse> ExecuteRequestAsync(
            RestRequest request,
            CancellationToken token = default)
        {
            var possibleBody = request.Parameters.GetParameters(ParameterType.RequestBody).Cast<BodyParameter>().AsEnumerable()
                .FirstOrDefault()
                ?.Value;

            RequestForwardingEnvelope requestEnvelope = new RequestForwardingEnvelope(
                ToHttpMethod(request.Method),
                request.Resource,
                possibleBody == null ? null : JsonConvert.SerializeObject(possibleBody),
                request.Files.ToEnvelopes(),
                request.Parameters.ToEnvelopes());

            // Note: this method is used in only one place, except for the unit tests, and its return is ignored.
            await this.ExecuteRawRequestAsync(
                requestEnvelope,
                token);

            return null;
        }

        /// <summary>
        /// Executes an async request and returns the response as JSON.
        /// </summary>
        /// <param name="method">Request method.</param>
        /// <param name="resource">Request resource url.</param>
        /// <param name="requestBody">Request body to be serialized.</param>
        /// <param name="token">Cancellation token for the operation.</param>
        public async Task<JToken> ExecuteRequestAsync(
            Method method,
            string resource,
            object requestBody = null,
            CancellationToken token = default)
        {
            if (method == Method.Get && requestBody != null)
            {
                throw new InvalidOperationException($"GET requests are not allowed to have a request body. Resource: {resource}. Body: {requestBody}");
            }

            RequestForwardingEnvelope requestEnvelope = new RequestForwardingEnvelope(
                ToHttpMethod(method),
                resource,
                requestBody == null ? null : JsonConvert.SerializeObject(requestBody),
                null,
                null);

            return await this.ExecuteRawRequestAsync(requestEnvelope, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes an async request and serializes the response to an object.
        /// </summary>
        /// <typeparam name="T">Type to serialize the response.</typeparam>
        /// <param name="method">Request method.</param>
        /// <param name="resource">Request resource url.</param>
        /// <param name="requestBody">Request body to be serialized.</param>
        /// <param name="token">Cancellation token for this operation.</param>
        public async Task<T> ExecuteRequestAsync<T>(
            Method method,
            string resource,
            object requestBody = null,
            CancellationToken token = default)
        {
            var result = await ExecuteRequestAsync(method, resource, requestBody, token).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(result.ToString(), Settings.JsonSerializerSettings);
        }

        /// <summary>
        /// Downloads file as a byte array.
        /// </summary>
        /// <param name="url">Url to the file location.</param>
        public async Task<byte[]> DownloadDataAsync(string url)
        {
            RequestForwardingEnvelope request = new RequestForwardingEnvelope(
                HttpMethod.Get,
                url);
            RestRequest forwardedRequest = new RestRequest(
                "/",
                Method.Post);
            forwardedRequest.AddJsonBody(request);
            var rawResponse = await this._remoteRestClient.ExecuteAsync(forwardedRequest);

            var rawContent = rawResponse.Content != null ? rawResponse.Content.Trim() : string.Empty;

            if (this._settings.EnableRequestTrace)
            {
                Trace.WriteLine($"[{request.Method}] Response for Url: {request.Resource}\n{rawContent}");
            }

            if (rawResponse.ResponseStatus != ResponseStatus.Completed)
            {
                if (!string.IsNullOrEmpty(rawResponse.ErrorMessage))
                {
                    throw new InvalidOperationException($"Error Message: {rawResponse.ErrorMessage}");
                }
                else
                {
                    throw new InvalidOperationException($"Request could not complete: {rawResponse.ResponseStatus}");
                }
            }

            var response = JsonConvert.DeserializeObject<ResponseForwardingEnvelope>(rawContent);

            if (response == null || !response.RequestSuccessful)
            {
                throw new InvalidOperationException($"Mediated request could not complete.");
            }

            return response.RawContent;
        }

        private HttpMethod ToHttpMethod(Method method)
        {
            if (method == Method.Get)
            {
                return HttpMethod.Get;
            }

            if (method == Method.Post)
            {
                return HttpMethod.Post;
            }

            if (method == Method.Put)
            {
                return HttpMethod.Put;
            }

            if (method == Method.Delete)
            {
                return HttpMethod.Delete;
            }

            if (method == Method.Head)
            {
                return HttpMethod.Head;
            }

            if (method == Method.Options)
            {
                return HttpMethod.Options;
            }

            if (method == Method.Search)
            {
                return HttpMethod.Trace;
            }

            throw new ArgumentDoesNotMatchException();
        }

        private async Task<JToken> ExecuteRawRequestAsync(RequestForwardingEnvelope request, CancellationToken cancellationToken)
        {
            RestRequest forwardedRequest = new RestRequest(
                "/",
                Method.Post);
            forwardedRequest.AddJsonBody(request);
            var rawResponse = await this._remoteRestClient.ExecuteAsync(forwardedRequest, cancellationToken);

            var rawContent = rawResponse.Content != null ? rawResponse.Content.Trim() : string.Empty;

            if (this._settings.EnableRequestTrace)
            {
                Trace.WriteLine($"[{request.Method}] Response for Url: {request.Resource}\n{rawContent}");
            }

            if (rawResponse.ResponseStatus != ResponseStatus.Completed)
            {
                if (!string.IsNullOrEmpty(rawResponse.ErrorMessage))
                {
                    throw new InvalidOperationException($"Error Message: {rawResponse.ErrorMessage}");
                }
                else
                {
                    throw new InvalidOperationException($"Request could not complete: {rawResponse.ResponseStatus}");
                }
            }

            var response = JsonConvert.DeserializeObject<ResponseForwardingEnvelope>(rawContent);

            if (response == null || !response.RequestSuccessful)
            {
                throw new InvalidOperationException($"Mediated request could not complete.");
            }

            var content = response.JsonBody != null ? response.JsonBody.Trim() : string.Empty;

            if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new System.Security.Authentication.AuthenticationException($"Response Content: {content}");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ResourceNotFoundException($"Response Content: {content}");
            }

            if ((int)response.StatusCode >= 400)
            {
                throw new InvalidOperationException($"Response Status Code: {(int)response.StatusCode}. Response Content: {content}");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return new JObject();
            }

            if (!content.StartsWith("{") && !content.StartsWith("["))
            {
                throw new InvalidOperationException($"Response was not recognized as JSON. Content: {content}");
            }

            JToken parsedContent;

            try
            {
                parsedContent = JToken.Parse(content);
            }
            catch (JsonReaderException ex)
            {
                throw new InvalidOperationException($"Failed to parse response as JSON. Content: {content}", ex);
            }

            if (parsedContent != null && parsedContent.Type == JTokenType.Object && parsedContent["errorMessages"] != null)
            {
                throw new InvalidOperationException(
                    $"Response reported error(s) from JIRA: {parsedContent["errorMessages"]}");
            }

            return parsedContent;
        }
    }
}