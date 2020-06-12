using System;

namespace CustEdPrioritizer
{
    /// <summary>
    /// Exception expressing an unwanted result during a POST or GET request.
    /// </summary>
    internal class AtlassianRequestException : ApplicationException
    {
        public string ReasonPhrase { get; set; }

        public int StatusCode { get; set; }

        public string ResponseContent { get; set; }

        public AtlassianRequestException(string message, int statusCode, string reasonPhrase, string responseContent)
            : base(message)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            ResponseContent = responseContent;
        }
    }
}