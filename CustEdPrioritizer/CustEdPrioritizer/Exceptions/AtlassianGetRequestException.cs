namespace CustEdPrioritizer
{
    /// <summary>
    /// Exception expressing an unwanted result during a GET request.
    /// </summary>
    internal class AtlassianGetRequestException : AtlassianRequestException
    {
        public AtlassianGetRequestException(string message, int statusCode, string reasonPhrase, string responseContent)
            : base(message, statusCode, reasonPhrase, responseContent)
            { }
    }
}
