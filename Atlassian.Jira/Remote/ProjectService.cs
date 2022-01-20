using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace Atlassian.Jira.Remote
{
    internal class ProjectService : IProjectService
    {
        private readonly Jira _jira;

        public ProjectService(Jira jira)
        {
            _jira = jira;
        }

        public async Task<IEnumerable<Project>> GetProjectsAsync(CancellationToken token = default)
        {
            var cache = _jira.Cache;
            if (!cache.Projects.Any())
            {
                var remoteProjects = await _jira.RestClient.ExecuteRequestAsync<RemoteProject[]>(Method.Get, "rest/api/2/project?expand=lead,url", null, token).ConfigureAwait(false);
                cache.Projects.TryAdd(remoteProjects.Select(p => new Project(_jira, p)));
            }

            return cache.Projects.Values;
        }

        public async Task<Project> GetProjectAsync(string projectKey, CancellationToken token = default)
        {
            var resource = string.Format("rest/api/2/project/{0}?expand=lead,url", projectKey);
            var remoteProject = await _jira.RestClient.ExecuteRequestAsync<RemoteProject>(Method.Get, resource, null, token).ConfigureAwait(false);
            return new Project(_jira, remoteProject);
        }
    }
}
