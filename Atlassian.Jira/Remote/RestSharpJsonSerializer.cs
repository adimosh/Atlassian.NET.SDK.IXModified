using System.IO;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers;

namespace Atlassian.Jira.Remote
{
    /// <summary>
    /// Taken from https://github.com/restsharp/RestSharp/blob/86b31f9adf049d7fb821de8279154f41a17b36f7/RestSharp/Serializers/JsonSerializer.cs
    /// </summary>
    public class RestSharpJsonSerializer : IRestSerializer
    {
        private readonly JsonSerializer _serializer;
        private readonly NewtonsoftJsonSerializer _wrapper;

        /// <summary>
        /// Default serializer
        /// </summary>
        public RestSharpJsonSerializer()
        : this(new JsonSerializer
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Include
        })
        {
        }

        /// <summary>
        /// Default serializer with overload for allowing custom Json.NET settings
        /// </summary>
        public RestSharpJsonSerializer(JsonSerializer serializer)
        {
            _serializer = serializer;
            _wrapper = new NewtonsoftJsonSerializer(this._serializer);
        }

        public string Serialize(Parameter parameter)
        {
            using (var stringWriter = new StringWriter())
            {
                using (var jsonTextWriter = new JsonTextWriter(stringWriter))
                {
                    jsonTextWriter.Formatting = Formatting.Indented;
                    jsonTextWriter.QuoteChar = '"';

                    _serializer.Serialize(jsonTextWriter, parameter.Value);

                    var result = stringWriter.ToString();
                    return result;
                }
            }
        }

        public ISerializer Serializer => this._wrapper;

        public IDeserializer Deserializer => this._wrapper;

        public string[] SupportedContentTypes => ContentType.JsonAccept;

        public DataFormat DataFormat => DataFormat.Json;

        internal class NewtonsoftJsonSerializer : ISerializer, IDeserializer
        {
            private readonly JsonSerializer _serializer;

            internal NewtonsoftJsonSerializer(JsonSerializer serializer)
            {
                _serializer = serializer;
            }

            public string Serialize(object obj)
            {
                using (var stringWriter = new StringWriter())
                {
                    using (var jsonTextWriter = new JsonTextWriter(stringWriter))
                    {
                        jsonTextWriter.Formatting = Formatting.Indented;
                        jsonTextWriter.QuoteChar = '"';

                        _serializer.Serialize(jsonTextWriter, obj);

                        var result = stringWriter.ToString();
                        return result;
                    }
                }
            }

            public string ContentType { get; set; }

            public T Deserialize<T>(RestResponse response)
            {
                using (var stringReader = new StringReader(response.Content))
                {
                    using (var jsonTextReader = new JsonTextReader(stringReader))
                    {
                        return _serializer.Deserialize<T>(jsonTextReader);
                    }
                }
            }
        }
    }
}
