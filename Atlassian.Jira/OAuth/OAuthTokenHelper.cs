using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using RestSharp;
using RestSharp.Authenticators;

namespace Atlassian.Jira.OAuth
{
    /// <summary>
    /// Helper to create and send request for the OAuth authentication process.
    /// </summary>
    public static class OAuthTokenHelper
    {
        /// <summary>
        /// Generate a request token for the OAuth authentication process.
        /// </summary>
        /// <param name="oAuthRequestTokenSettings"> The request token settings.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>The <see cref="OAuthRequestToken" /> containing the request token, the consumer token and the authorize url.</returns>
        public static Task<OAuthRequestToken> GenerateRequestTokenAsync(OAuthRequestTokenSettings oAuthRequestTokenSettings, CancellationToken cancellationToken = default)
        {
            var authenticator = OAuth1Authenticator.ForRequestToken(
                oAuthRequestTokenSettings.ConsumerKey,
                oAuthRequestTokenSettings.ConsumerSecret,
                oAuthRequestTokenSettings.CallbackUrl);

            authenticator.SignatureMethod = oAuthRequestTokenSettings.SignatureMethod.ToOAuthSignatureMethod();

            var restClient = new RestClient(oAuthRequestTokenSettings.Url)
            {
                Authenticator = authenticator
            };

            return GenerateRequestTokenAsync(
                restClient,
                oAuthRequestTokenSettings.Url,
                oAuthRequestTokenSettings.RequestTokenUrl,
                oAuthRequestTokenSettings.AuthorizeUrl,
                cancellationToken);
        }

        /// <summary>
        /// Generate a request token for the OAuth authentication process.
        /// </summary>
        /// <param name="restClient">The rest client.</param>
        /// <param name="baseUrl">The base Url for the rest client.</param>
        /// <param name="requestTokenUrl">The relative url to request the token to Jira.</param>
        /// <param name="authorizeTokenUrl">The relative url to authorize the token.</param>
        /// <param name="cancellationToken">Cancellation token for this operation.</param>
        /// <returns>The <see cref="OAuthRequestToken" /> containing the request token, the consumer token and the authorize url.</returns>
        public static async Task<OAuthRequestToken> GenerateRequestTokenAsync(
            RestClient restClient,
            string baseUrl,
            string requestTokenUrl,
            string authorizeTokenUrl,
            CancellationToken cancellationToken = default)
        {
            var requestTokenResponse = await restClient.ExecuteAsync(
                new RestRequest(requestTokenUrl, Method.Post),
                cancellationToken).ConfigureAwait(false);

            if (requestTokenResponse.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            var requestTokenQuery = HttpUtility.ParseQueryString(requestTokenResponse.Content.Trim());

            var oauthToken = requestTokenQuery["oauth_token"];
            var authorizeUri = $"{baseUrl}/{authorizeTokenUrl}?oauth_token={oauthToken}";

            return new OAuthRequestToken(
                authorizeUri,
                oauthToken,
                requestTokenQuery["oauth_token_secret"],
                requestTokenQuery["oauth_callback_confirmed"]);
        }

        /// <summary>
        /// Obtain the access token from an authorized request token.
        /// </summary>
        /// <param name="oAuthAccessTokenSettings">The settings to obtain the access token.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The access token from Jira.
        /// Return null if the token was not returned by Jira or the token secret for the request token and the access token don't match.</returns>
        public static Task<string> ObtainAccessTokenAsync(OAuthAccessTokenSettings oAuthAccessTokenSettings, CancellationToken cancellationToken)
        {
            var restClient = new RestClient(oAuthAccessTokenSettings.Url)
            {
                Authenticator = OAuth1Authenticator.ForAccessToken(
                    oAuthAccessTokenSettings.ConsumerKey,
                    oAuthAccessTokenSettings.ConsumerSecret,
                    oAuthAccessTokenSettings.OAuthRequestToken,
                    oAuthAccessTokenSettings.OAuthTokenSecret,
                    oAuthAccessTokenSettings.SignatureMethod.ToOAuthSignatureMethod())
            };

            return ObtainAccessTokenAsync(
                restClient,
                oAuthAccessTokenSettings.AccessTokenUrl,
                oAuthAccessTokenSettings.OAuthTokenSecret,
                cancellationToken);
        }

        /// <summary>
        /// Obtain the access token from an authorized request token.
        /// </summary>
        /// <param name="restClient">The rest client.</param>
        /// <param name="accessTokenUrl">The relative path to the url to request the access token to Jira.</param>
        /// <param name="oAuthTokenSecret">The OAuth token secret generated by Jira.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The access token from Jira.
        /// Return null if the token was not returned by Jira or the token secret for the request token and the access token don't match.</returns>
        public static async Task<string> ObtainAccessTokenAsync(
            RestClient restClient,
            string accessTokenUrl,
            string oAuthTokenSecret,
            CancellationToken cancellationToken)
        {
            var accessTokenResponse = await restClient.ExecuteAsync(
                new RestRequest(accessTokenUrl, Method.Post),
                cancellationToken).ConfigureAwait(false);

            if (accessTokenResponse.StatusCode != HttpStatusCode.OK)
            {
                // The token has not been authorize or something went wrong
                return null;
            }

            var accessTokenQuery = HttpUtility.ParseQueryString(accessTokenResponse.Content.Trim());

            if (oAuthTokenSecret != accessTokenQuery["oauth_token_secret"])
            {
                // The request token secret and access token secret do not match.
                return null;
            }

            return accessTokenQuery["oauth_token"];
        }
    }
}
