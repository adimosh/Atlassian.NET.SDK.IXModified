﻿using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers;

namespace Atlassian.Jira.Remote
{
    /// <summary>
    /// Implements the IJiraRestClient interface using RestSharp.
    /// </summary>
    public class JiraRestClient : IJiraRestClient
    {
        private readonly string _url;
        private readonly RestClient _restClient;
        private readonly JiraRestClientSettings _clientSettings;

        /// <summary>
        /// Creates a new instance of the JiraRestClient class.
        /// </summary>
        /// <param name="url">Url to the JIRA server.</param>
        /// <param name="username">Username used to authenticate.</param>
        /// <param name="password">Password used to authenticate.</param>
        /// <param name="settings">Settings to configure the rest client.</param>
        public JiraRestClient(string url, string username = null, string password = null, JiraRestClientSettings settings = null)
            : this(url, new HttpBasicAuthenticator(username, password), settings)
        {
        }

        /// <summary>
        /// Creates a new instance of the JiraRestClient class.
        /// </summary>
        /// <param name="url">The url to the JIRA server.</param>
        /// <param name="authenticator">The authenticator used by RestSharp.</param>
        /// <param name="settings">The settings to configure the rest client.</param>
        protected JiraRestClient(string url, IAuthenticator authenticator, JiraRestClientSettings settings = null)
        {
            _url = url.EndsWith("/") ? url : url + "/";
            _clientSettings = settings ?? new JiraRestClientSettings();

            _restClient = new RestClient(
                new RestClientOptions(_url)
                {
                    Proxy = _clientSettings.Proxy,
                });

            this._restClient.Authenticator = authenticator;
            this._restClient.UseSerializer(
                () => new RestSharpJsonSerializer(JsonSerializer.Create(Settings.JsonSerializerSettings)));
        }

        /// <summary>
        /// Rest sharp client used to issue requests.
        /// </summary>
        internal RestClient RestSharpClient => _restClient;

        /// <summary>
        /// Url to the JIRA server.
        /// </summary>
        public string Url => this._url;

        /// <summary>
        /// Settings to configure the rest client.
        /// </summary>
        public JiraRestClientSettings Settings => _clientSettings;

        /// <summary>
        /// Executes an async request and serializes the response to an object.
        /// </summary>
        public async Task<T> ExecuteRequestAsync<T>(Method method, string resource, object requestBody = null, CancellationToken token = default)
        {
            var result = await ExecuteRequestAsync(method, resource, requestBody, token).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(result.ToString(), Settings.JsonSerializerSettings);
        }

        /// <summary>
        /// Executes an async request and returns the response as JSON.
        /// </summary>
        public async Task<JToken> ExecuteRequestAsync(Method method, string resource, object requestBody = null, CancellationToken token = default)
        {
            if (method == Method.Get && requestBody != null)
            {
                throw new InvalidOperationException($"GET requests are not allowed to have a request body. Resource: {resource}. Body: {requestBody}");
            }

            var request = new RestRequest
            {
                Method = method,
                Resource = resource,
                RequestFormat = DataFormat.Json
            };

            if (requestBody is string)
            {
                _ = request.AddParameter(new BodyParameter("", requestBody, ContentType.Json, DataFormat.Json));
            }
            else if (requestBody != null)
            {
                request.AddJsonBody(requestBody);
            }

            LogRequest(request, requestBody);
            var response = await this.ExecuteRawRequestAsync(request, token).ConfigureAwait(false);
            return GetValidJsonFromResponse(request, response);
        }

        /// <summary>
        /// Executes a request with logging and validation.
        /// </summary>
        public async Task<RestResponse> ExecuteRequestAsync(RestRequest request, CancellationToken token = default)
        {
            LogRequest(request);
            var response = await this.ExecuteRawRequestAsync(request, token).ConfigureAwait(false);
            GetValidJsonFromResponse(request, response);
            return response;
        }

        /// <summary>
        /// Executes a raw request.
        /// </summary>
        protected virtual Task<RestResponse> ExecuteRawRequestAsync(RestRequest request, CancellationToken token)
        {
            return _restClient.ExecuteAsync(request, token);
        }

        /// <summary>
        /// Downloads file as a byte array.
        /// </summary>
        /// <param name="url">Url to the file location.</param>
        public Task<byte[]> DownloadDataAsync(string url)
        {
            return _restClient.DownloadDataAsync(new RestRequest(url));
        }

        private void LogRequest(RestRequest request, object body = null)
        {
            if (this._clientSettings.EnableRequestTrace)
            {
                Trace.WriteLine($"[{request.Method}] Request Url: {request.Resource}");

                if (body != null)
                {
                    Trace.WriteLine(
                        $"[{request.Method}] Request Data: {JsonConvert.SerializeObject(body, new JsonSerializerSettings() { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore })}");
                }
            }
        }

        private JToken GetValidJsonFromResponse(RestRequest request, RestResponse response)
        {
            var content = response.Content != null ? response.Content.Trim() : string.Empty;

            if (this._clientSettings.EnableRequestTrace)
            {
                Trace.WriteLine($"[{request.Method}] Response for Url: {request.Resource}\n{content}");
            }

            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                throw new InvalidOperationException($"Error Message: {response.ErrorMessage}");
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new System.Security.Authentication.AuthenticationException(string.Format("Response Content: {0}", content));
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ResourceNotFoundException($"Response Content: {content}");
            }
            else if ((int)response.StatusCode >= 400)
            {
                throw new InvalidOperationException($"Response Status Code: {(int)response.StatusCode}. Response Content: {content}");
            }
            else if (string.IsNullOrWhiteSpace(content))
            {
                return new JObject();
            }
            else if (!content.StartsWith("{") && !content.StartsWith("["))
            {
                throw new InvalidOperationException(string.Format("Response was not recognized as JSON. Content: {0}", content));
            }
            else
            {
                JToken parsedContent;

                try
                {
                    parsedContent = JToken.Parse(content);
                }
                catch (JsonReaderException ex)
                {
                    throw new InvalidOperationException(string.Format("Failed to parse response as JSON. Content: {0}", content), ex);
                }

                if (parsedContent != null && parsedContent.Type == JTokenType.Object && parsedContent["errorMessages"] != null)
                {
                    throw new InvalidOperationException(string.Format("Response reported error(s) from JIRA: {0}", parsedContent["errorMessages"].ToString()));
                }

                return parsedContent;
            }
        }
    }
}
