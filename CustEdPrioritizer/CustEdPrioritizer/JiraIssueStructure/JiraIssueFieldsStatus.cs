using Newtonsoft.Json;

namespace CustEdPrioritizer
{
    /// <summary>
    /// Represents the data structure of a JIRA issue field status for JSON serialization.
    /// </summary>
    public class JiraIssueFieldsStatus
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
