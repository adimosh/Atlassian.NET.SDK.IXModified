using System;
using System.Threading.Tasks;
using Atlassian.Jira.Remote;

namespace Atlassian.Jira
{
    /// <summary>
    /// An attachment associated with an issue
    /// </summary>
    public class Attachment
    {
        private readonly Jira _jira;

        /// <summary>
        /// Creates a new instance of an Attachment from a remote entity.
        /// </summary>
        /// <param name="jira">Object used to interact with JIRA.</param>
        /// <param name="remoteAttachment">Remote attachment entity.</param>
        public Attachment(Jira jira, RemoteAttachment remoteAttachment)
        {
            _jira = jira;

            AuthorUser = remoteAttachment.authorUser;
            CreatedDate = remoteAttachment.created;
            FileName = remoteAttachment.filename;
            MimeType = remoteAttachment.mimetype;
            FileSize = remoteAttachment.filesize;
            Id = remoteAttachment.id;
        }

        /// <summary>
        /// Id of attachment
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Author of attachment (user that uploaded the file)
        /// </summary>
        public string Author => AuthorUser?.InternalIdentifier;

        /// <summary>
        /// User object of the author of attachment.
        /// </summary>
        public JiraUser AuthorUser { get; }

        /// <summary>
        /// Date of creation
        /// </summary>
        public DateTime? CreatedDate { get; }

        /// <summary>
        /// File name of the attachment
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Mime type
        /// </summary>
        public string MimeType { get; }

        /// <summary>
        /// File size
        /// </summary>
        public long? FileSize { get; }

        /// <summary>
        /// Downloads attachment as a byte array.
        /// </summary>
        public Task<byte[]> DownloadData()
        {
            var url = GetRequestUrl();

            return _jira.RestClient.DownloadDataAsync(url);
        }

        private string GetRequestUrl()
        {
            if (string.IsNullOrEmpty(_jira.Url))
            {
                throw new InvalidOperationException("Unable to download attachment, JIRA url has not been set.");
            }

            return string.Format("{0}secure/attachment/{1}/{2}",
                _jira.Url.EndsWith("/") ? _jira.Url : _jira.Url + "/",
                this.Id,
                this.FileName);
        }
    }
}
