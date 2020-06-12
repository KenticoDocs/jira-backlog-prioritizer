using Newtonsoft.Json;

namespace CustEdPrioritizer
{
    /// <summary>
    /// Represents the data structure of a JIRA issue for JSON serialization.
    /// </summary>
    public class JiraIssue
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("fields")]
        public JiraIssueFields Fields { get; set; }
    }
}
