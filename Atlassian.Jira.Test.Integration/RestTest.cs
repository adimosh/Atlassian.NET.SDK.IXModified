using Atlassian.Jira.Remote;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Atlassian.Jira.Test.Integration
{
    public class CookiesRestClient : JiraRestClient
    {
        private readonly IAuthenticator _authenticator;

        public CookiesRestClient(string url, string user, string password)
            : base(url, user, password)
        {
            RestSharpClient.Authenticator = null;
            _authenticator = new HttpBasicAuthenticator(user, password);
        }

        protected override async Task<RestResponse> ExecuteRawRequestAsync(RestRequest request, CancellationToken token)
        {
            var response = await this.RestSharpClient.ExecuteAsync(request, token).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                RestSharpClient.Authenticator = _authenticator;
                response = await this.RestSharpClient.ExecuteAsync(request, token).ConfigureAwait(false);
                RestSharpClient.Authenticator = null;
            }

            return response;
        }
    }

    public class RestTest
    {
        private readonly Random _random = new Random();

        [Fact]
        public async Task CanUseCustomRestClient()
        {
            var restClient = new CookiesRestClient(JiraProvider.HOST, JiraProvider.USERNAME, JiraProvider.PASSWORD);
            var jira = Jira.CreateRestClient(restClient);

            var issue = await jira.Issues.GetIssueAsync("TST-1");
            Assert.Equal("Sample bug in Test Project", issue.Summary);

            var types = await jira.IssueTypes.GetIssueTypesAsync();
            Assert.NotEmpty(types);
        }

        [Theory]
        [ClassData(typeof(JiraProvider))]
        public void ExecuteRestRequest(Jira jira)
        {
            var users = jira.RestClient.ExecuteRequestAsync<JiraNamedResource[]>(Method.Get, "rest/api/2/user/assignable/multiProjectSearch?projectKeys=TST").Result;

            Assert.True(users.Length >= 2);
            Assert.Contains(users, u => u.Name == "admin");
        }

        [Theory]
        [ClassData(typeof(JiraProvider))]
        public void ExecuteRawRestRequest(Jira jira)
        {
            var issue = new Issue(jira, "TST")
            {
                Type = "1",
                Summary = "Test Summary " + _random.Next(int.MaxValue),
                Assignee = "admin"
            };

            issue.SaveChanges();

            var rawBody = $"{{ \"jql\": \"Key=\\\"{issue.Key.Value}\\\"\" }}";
            var json = jira.RestClient.ExecuteRequestAsync(Method.Post, "rest/api/2/search", rawBody).Result;

            Assert.Equal(issue.Key.Value, json["issues"][0]["key"].ToString());
        }

        [Fact]
        public async Task WillThrowErrorIfSiteIsUnreachable()
        {
            var jira = Jira.CreateRestClient("http://farmasXXX.atlassian.net");

            var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() => jira.Issues.GetIssueAsync("TST-1"));
        }
    }
}
