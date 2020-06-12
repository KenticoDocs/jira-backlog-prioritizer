using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CustEdPrioritizer
{
    /// <summary>
    /// Provides methods for communicating with the JIRA and Confluence REST API using an <see cref="HttpClient"/>.
    /// </summary>
    public class AtlassianConnector
    {
        /// <summary>
        /// Base <see cref="Uri"/> of the REST service.
        /// </summary>
        public Uri BaseServiceUri { get; private set; }

        /// <summary>
        /// Authentication header used for authorization of REST requests.
        /// </summary>
        public AuthenticationHeaderValue AuthenticationHeader { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="baseServiceUri">Base <see cref="Uri"/> of the REST service.</param>
        /// <param name="authenticationHeader">Authentication header used for authorization of REST requests.</param>
        public AtlassianConnector(Uri baseServiceUri, AuthenticationHeaderValue authenticationHeader)
        {
            BaseServiceUri = baseServiceUri;
            AuthenticationHeader = authenticationHeader;
        }

        /// <summary>
        /// Creates a new <see cref="HttpClient"/> instance with a predefined base address and authorization header.
        /// </summary>
        /// <returns><see cref="HttpClient"/> instance with predefined properties.</returns>
        public HttpClient GetHttpClient()
        {
            HttpClient client = new HttpClient
            {
                // Sets the base URL of the REST request.
                BaseAddress = BaseServiceUri
            };

            // Authorizes the request using the Basic authentication.
            client.DefaultRequestHeaders.Authorization = AuthenticationHeader;

            // Specifes that the call must be TLS 1.2.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            return client;
        }

        /// <summary>
        /// Creates a GET request using the <see cref="HttpClient"/> connected to the Atlassian JIRA/Confluence REST service.
        /// </summary>
        /// <param name="encodedUrl">Encoded URL to where the GET request is sent.</param>
        /// <returns>String read from the content of the GET response.</returns
        public async Task<string> GetRequestAsync(string encodedUrl)
        {
            if (encodedUrl == null)
            {
                throw new NullReferenceException("An exception occurred during the GET request - Provided URL was null.");
            }

            using (HttpClient client = GetHttpClient())
            {
                // Sends the GET request and sets the 'HttpResponseMessage'.
                HttpResponseMessage response = await client.GetAsync(encodedUrl);

                // Checks whether the response returned with a success status code.
                if (!response.IsSuccessStatusCode)
                {
                    // Throws an exception when the GET request was not accepted by the REST service.
                    throw new AtlassianGetRequestException("An exception occurred during the GET request", (Int32)response.StatusCode, response.ReasonPhrase, await response.Content.ReadAsStringAsync());
                }

                // Returns the GET request response.
                return await response.Content.ReadAsStringAsync();
            }
        }
        
        /// <summary>
        /// Creates a PUT request using the <see cref="HttpClient"/> connected to the Atlassian JIRA/Confluence service.
        /// </summary>
        /// <param name="encodedUrl">Encoded URL to where the PUT request is sent.</param>
        /// <param name="jsonData">Serialized data to JSON Sent with the PUT request.</param>
        /// <returns>String read from the content of the PUT response.</returns>
        public async Task<string> PutRequestAsync(string encodedUrl, string jsonData)
        {
            if (encodedUrl == null || jsonData == null)
            {
                throw new NullReferenceException("An exception occurred during the PUT request - Some of provided arguments were null.");
            }

            using (HttpClient client = GetHttpClient())
            {
                // Prepares the PUT request.
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, encodedUrl)
                {
                    Content = new StringContent(jsonData, Encoding.UTF8, "application/json")
                };

                // Sends the PUT request and sets the 'HttpResponseMessage'.
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage);
            
                // Checks whether the response returned with a success status code.
                if (!responseMessage.IsSuccessStatusCode)
                {
                    // Throws an exception when the PUT request was not accepted by the REST service.
                    throw new AtlassianPutRequestException("An exception occurred during the PUT request", (Int32)responseMessage.StatusCode, responseMessage.ReasonPhrase, await responseMessage.Content.ReadAsStringAsync());
                }

                // Returns the response data of the PUT request as a String instance.
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }
    }
}