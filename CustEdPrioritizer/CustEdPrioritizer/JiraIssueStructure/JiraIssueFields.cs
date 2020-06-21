using System;
using Newtonsoft.Json;

namespace CustEdPrioritizer
{
    /// <summary>
    /// Represents the data structure of a JIRA issue field for JSON serialization.
    /// </summary>
    public class JiraIssueFields
    {
        // DUE DATE
        [JsonProperty("duedate")]
        private DateTime? DueDateOriginal
        {
            set
            {
                DueDate = value ?? default(DateTime);
            }
        }

        public DateTime DueDate { get; private set; }

        // ESTIMATE
        [JsonProperty("customfield_10004")]
        private double? EstimateOriginal
        {
            set
            {
                Estimate = value ?? 0;
            }
        }

        public double Estimate { get; private set; }

        // IMPACT
        [JsonProperty("customfield_19148")]
        private double? ImpactOriginal
        {
            set
            {
                Impact = value ?? 0;
            }
        }
        public double Impact { get; private set; }

        // USERBASE
        [JsonProperty("customfield_19149")]
        private double? UserbaseOriginal
        {
            set
            {
                Userbase = value ?? 0;
            }
        }

        public double Userbase { get; private set; }


        // STRATEGY
        [JsonProperty("customfield_19150")]
        private double? StrategyOriginal
        {
            set
            {
                Strategy = value ?? 0;
            }
        }

        public double Strategy { get; private set; }

        // EXPERIMENT
        [JsonProperty("customfield_19151")]
        private double? ExperimentOriginal
        {
            set
            {
                Experiment = value ?? 0;
            }
        }

        public double Experiment { get; private set; }

        // TOTAL
        [JsonProperty("customfield_19152")]
        private double? TotalOriginal
        {
            set
            {
                Total = value ?? 0;
            }
        }

        public double Total { get; set; }

        // EPIC
        [JsonProperty("customfield_10008")]
        public string Epic { get; set; }

        // STATUS
        [JsonProperty("status")]
        private JiraIssueFieldsStatus StatusOriginal { get; set; }

        public string Status
        {
            get
            {
                return StatusOriginal.Name;
            }
        }
    }
}
