﻿namespace Atlassian.Jira.OAuth
{
    /// <summary>
    /// Access token settings to help obtain the access token.
    /// </summary>
    public class OAuthAccessTokenSettings
    {
        /// <summary>
        /// The default relative URL to request an access token.
        /// </summary>
        public const string DefaultAccessTokenUrl = "plugins/servlet/oauth/access-token";

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuthAccessTokenSettings"/> class.
        /// </summary>
        /// <param name="url">The URL of the Jira instance to request to.</param>
        /// <param name="consumerKey">The consumer key provided by the Jira application link.</param>
        /// <param name="consumerSecret">The consumer private key in XML format.</param>
        /// <param name="oAuthRequestToken">The OAuth request token generated by Jira.</param>
        /// <param name="oAuthTokenSecret">The OAuth token secret generated by Jira.</param>
        /// <param name="signatureMethod">The signature method used to sign the request.</param>
        /// <param name="accessTokenUrl">The relative URL to request the access token to Jira.</param>
        public OAuthAccessTokenSettings(
            string url,
            string consumerKey,
            string consumerSecret,
            string oAuthRequestToken,
            string oAuthTokenSecret,
            JiraOAuthSignatureMethod signatureMethod = JiraOAuthSignatureMethod.RsaSha1,
            string accessTokenUrl = DefaultAccessTokenUrl)
        {
            Url = url;
            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
            OAuthRequestToken = oAuthRequestToken;
            OAuthTokenSecret = oAuthTokenSecret;
            SignatureMethod = signatureMethod;
            AccessTokenUrl = accessTokenUrl;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuthAccessTokenSettings"/> class.
        /// </summary>
        /// <param name="oAuthRequestTokenSettings">The settings used to generate the request token.</param>
        /// <param name="oAuthRequestToken">The request token object returned by <see cref="OAuthTokenHelper.GenerateRequestTokenAsync" />.</param>
        public OAuthAccessTokenSettings(
            OAuthRequestTokenSettings oAuthRequestTokenSettings,
            OAuthRequestToken oAuthRequestToken)
        {
            Url = oAuthRequestTokenSettings.Url;
            ConsumerKey = oAuthRequestTokenSettings.ConsumerKey;
            ConsumerSecret = oAuthRequestTokenSettings.ConsumerSecret;
            SignatureMethod = oAuthRequestTokenSettings.SignatureMethod;
            OAuthRequestToken = oAuthRequestToken.OAuthToken;
            OAuthTokenSecret = oAuthRequestToken.OAuthTokenSecret;
            AccessTokenUrl = DefaultAccessTokenUrl;
        }

        /// <summary>
        /// Gets the URL of the Jira instance to request to.
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// Gets the consumer key provided by the Jira application link.
        /// </summary>
        public string ConsumerKey { get; }

        /// <summary>
        /// Gets the consumer private key in XML format.
        /// </summary>
        public string ConsumerSecret { get; }

        /// <summary>
        /// Gets the OAuth request token generated by Jira.
        /// </summary>
        public string OAuthRequestToken { get; }

        /// <summary>
        /// Gets the OAuth token secret generated by Jira.
        /// </summary>
        public string OAuthTokenSecret { get; }

        /// <summary>
        /// Gets the signature method used to sign the request.
        /// </summary>
        public JiraOAuthSignatureMethod SignatureMethod { get; }

        /// <summary>
        /// Gets the relative URL to request the access token.
        /// </summary>
        public string AccessTokenUrl { get; }
    }
}