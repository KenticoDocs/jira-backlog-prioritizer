using System.Collections.Generic;
using Newtonsoft.Json;

namespace CustEdPrioritizer
{
    /// <summary>
    /// Represents the data structure of a result set returned when finding/getting issues using the JIRA REST service.
    /// </summary>
    internal class JiraSeachResults
    {
        [JsonProperty("total")]
        public int Count { get; set; }

        [JsonProperty("maxResults")]
        public int Size { get; set; }

        [JsonProperty("startAt")]
        public int Start { get; set; }

        [JsonProperty("issues")]
        public List<JiraIssue> Results { get; set; }
    }
}