using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

namespace CustEdPrioritizer
{
    /// <summary>
    /// Fills in fields (with the intention of prioritization) into Jira sprint issues.
    /// </summary>
    class JiraManager
    {
        private const int JIRA_PAGE_RESULTS_REST_LIMIT = 100;

        private const string JQL_FILTER_PATH = "rest/api/2/search?jql=sprint={0}+AND+status!=Closed+AND+issuetype+NOT+IN+(Epic,Sub-task)&maxResults={1}&startAt={2}&fields={3}";
        private const string FIELDS_IDS = "duedate,customfield_10004,customfield_19148,customfield_19149,customfield_19150,customfield_19151,customfield_19152,customfield_10008,status";

        private const string ISSUE_BY_ID_PATH = "rest/api/2/issue/";
        private const string TOTAL_FIELD_CONTENT = "{{\"fields\":{{\"customfield_19152\":{0}}}}}";

        /// <summary>
        /// AtlassianConnector instance used to communicate with the JIRA and Confluence REST service.
        /// </summary>
        public AtlassianConnector Connector { get; private set; }

        /// <summary>
        /// Writer instance used for console output and logging of messages.
        /// </summary>
        public FileAndConsoleWriter Writer { get; private set; }

        /// <summary>
        /// Identifies the sprint from which the issues are loaded.
        /// </summary>
        public int SprintId { get; private set; }

        /// <summary>
        /// Email address from which notification emails are sent. Also, the username for signing in.
        /// </summary>
        public string EmailFrom { get; set; }

        /// <summary>
        /// Email address to which notification emails are sent.
        /// </summary>
        public string EmailTo { get; set; }

        /// <summary>
        /// Password to the user account specified by the <see cref="EmailTo"/>
        /// </summary>
        public string EmailFromPassword { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connector">Connector to the Atlassian services that keeps the connection with the program.</param>
        /// <param name="writer">Instance of the log writer.</param>
        /// <param name="sprintId">Jira sprint ID.</param>
        /// <param name="emailFrom">Email address from which notification emails are sent. Also, the username for signing in.</param>
        /// <param name="emailTo">Email address to which notification emails are sent.</param>
        /// <param name="emailFromPassword">Password to the user account specified by the <see cref="EmailTo"/></param>
        public JiraManager(AtlassianConnector connector, FileAndConsoleWriter writer, int sprintId, string emailFrom, string emailTo, string emailFromPassword)
        {
            Connector = connector;
            Writer = writer;
            SprintId = sprintId;
            EmailFrom = emailFrom;
            EmailTo = emailTo;
            EmailFromPassword = emailFromPassword;
        }

        /// <summary>
        /// Fills in the set Jira sprint issues according to the defined rules.
        /// </summary>
        public async Task PrioritizeBacklogAsync()
        {
            // Retrieves the issues in the sprint asynchroniously.
            Task<JiraSeachResults> getBacklogIssues = GetUnclosedSprintIssuesAsync();

            // Awaits for retrieving the sprint issues.
            JiraSeachResults backlogIssues = await getBacklogIssues;
            if (backlogIssues == null)
            {
                Writer.WriteLine($"There are no issues in sprint {SprintId}.");
                MailSender.SendMail(EmailFrom, EmailTo, EmailFromPassword, "CTC prioritization feels kinda weird", $"The tool hasn't found any issue in the given sprint (ID: {SprintId}). Is this really the sprint you want to prioritize?");
                return;
            }
            Writer.WriteLine($"There are {backlogIssues.Count} issues to prioritize in sprint {SprintId}.");
            Writer.WriteLine();

            // Gets a non-duplicated list of epics
            HashSet<string> epicKeys = new HashSet<string>();

            foreach (JiraIssue issue in backlogIssues.Results)
            {
                if (issue.Fields.Epic != null && issue.Fields.Epic.StartsWith("CTC-"))
                {
                    epicKeys.Add(issue.Fields.Epic);
                }
            }

            // Retrieves the issues' epics asynchronously.
            var epicIssues = epicKeys.Select(epic => GetEpic(epic)).ToList();

            // Waits for the tasks creating pages and count their successfulness.
            JiraIssue[] epics = await Task.WhenAll(epicIssues);


            // Prioritizes the issues asynchronously.
            var prioritizedIssues = backlogIssues.Results.Select(issue => ProcessIssueAsync(issue, issue.Fields.Epic != null && issue.Fields.Epic.StartsWith("CTC-") ? epics.Where(epic => epic.Key == issue.Fields.Epic).FirstOrDefault() : null)).ToList();

            // Waits for the tasks creating pages and count their successfulness.
            Tuple<int, string>[] prioritizedResults = await Task.WhenAll(prioritizedIssues);

            /*
            // For debugging async-await
            List<String> prioritizedIssues = new List<string>();
            foreach (JiraIssue issue in backlogIssues.Results)
            {
                prioritizedIssues.Add(await ProcessIssueAsync(issue));
            }
            string[] prioritizedResults = prioritizedIssues.ToArray();
            */

            // Write out the final results.
            int failedIssueNumber = prioritizedResults.Where(x => x.Item1 == 0).Count();

            Writer.WriteLine();
            Writer.WriteLine("------------------------");
            Writer.WriteLine("FINAL RESULTS");
            Writer.WriteLine($"Number of all issues: {backlogIssues.Count}");
            Writer.WriteLine();
            Writer.WriteLine($"Number of updated issues: {prioritizedResults.Where(x => x.Item1 == 1).Count()}");
            Writer.WriteLine($"Number of issues that didn't need to be updated: {prioritizedResults.Where(x => x.Item1 == 2).Count()}");
            Writer.WriteLine($"Number of issues whose update failed: {failedIssueNumber}");
            
            // If there were any failures, they are listed and a notification email is sent.
            if (failedIssueNumber > 0)
            {
                StringBuilder failedIssues = new StringBuilder();
                foreach (Tuple<int, string> t in prioritizedResults)
                {
                    if (t.Item1 == 0)
                    {
                        failedIssues.AppendLine(t.Item2);
                        failedIssues.AppendLine();
                    }
                }

                Writer.WriteLine("");
                Writer.WriteLine("Issues whose update failed:");
                Writer.WriteLine(failedIssues.ToString());

                MailSender.SendMail(EmailFrom, EmailTo, EmailFromPassword, "CTC prioritization ended with errors", $"Prioritization ended with the following errors in updates: {failedIssues.ToString()}");
            }
        }

        /// <summary>
        /// Gets an epic specified by its key.
        /// </summary>
        /// <param name="epicKey">The Jira key of the epic issue.</param>
        /// <returns>The Jira issue with the retrieved fields.</returns>
        private async Task<JiraIssue> GetEpic(string epicKey)
        {
            // Prepares the resource path of the GET request retrieving the epic
            string resourcePath = String.Format(ISSUE_BY_ID_PATH + epicKey);

            string responseContent;

            try
            {
                // Gets the JSON content from the GET request.
                responseContent = await Connector.GetRequestAsync(resourcePath);
            }
            catch (NullReferenceException ex)
            {
                Writer.WriteLine("Exception: Retrieving of the JIRA epics failed ({0}).", ex.Message);
                return null;
            }
            catch (AtlassianGetRequestException ex)
            {
                Writer.WriteLine("Exception: Retrieving of the JIRA epics failed ({0}: {1} - {2}).", ex.Message, ex.StatusCode, ex.ReasonPhrase);
                Writer.WriteLine(ex.ResponseContent);
                return null;
            }

            // Deserializes the request data into an object.
            return JsonConvert.DeserializeObject<JiraIssue>(responseContent);
        }

        /// <summary>
        /// Ensures prioritizing issues, i.e. calculating and filling in the total prioritization number.
        /// </summary>
        /// <param name="issue">Jira issue to be calculated.</param>
        /// <returns>Result's code (0=update failed, 1=update successful, 2=update not needed) and the issue key (with the error message in case of code 0).</returns>
        private async Task<Tuple<int, string>> ProcessIssueAsync(JiraIssue issue, JiraIssue epic = null)
        {
            JiraIssueFields issueValues = issue.Fields;

            // Calculation begins here:
            const double estimateProportion = 15;
            const double impactProportion = 15;
            const double userbaseProportion = 15;
            const double strategyProportion = 20;
            const double dueProportion = 35;

            const double epicImpactProportion = 30;
            const double epicUserbaseProportion = 30;
            const double epicStrategyProportion = 40;

            const double estimateConst = 0.3;
            double estimate = Math.Pow(estimateConst, issueValues.Estimate) / Math.Pow(estimateConst, 0.1) * estimateProportion;
            double impact = issueValues.Impact / 3 * impactProportion;
            double userbase = issueValues.Userbase / 3 * userbaseProportion;
            
            double manualSubtotal = issueValues.Strategy / 3 * strategyProportion;
            double epicSubtotal = 0;
            if (epic != null && epic.Fields.Status.ToLower() == "in progress")
            {
                double epicImpact = epic.Fields.Impact / 3 * epicImpactProportion;
                double epicUserbase = epic.Fields.Userbase / 3 * epicUserbaseProportion;
                double epicStrategy = epic.Fields.Strategy / 3 * epicStrategyProportion;

                epicSubtotal = epicImpact + epicUserbase + epicStrategy / 100 * strategyProportion;
            }
            double strategy = epicSubtotal > manualSubtotal ? epicSubtotal : manualSubtotal;

            double due;
            if (issueValues.DueDate == default(DateTime))
            {
                due = 0;
            }
            else
            {
                double numberOfDays = (issueValues.DueDate - DateTime.Now).TotalDays;

                switch (numberOfDays)
                {
                    case var days when days <= issueValues.Estimate:
                        due = dueProportion;
                        break;
                    case var days when days > issueValues.Estimate * 5:
                        due = 0;
                        break;
                    default:
                        due = issueValues.Estimate * (dueProportion / numberOfDays) / 2;
                        break;
                }
            }

            double total = Math.Round(estimate + impact + userbase + strategy + due, 2);
            // Calculation ends here.

            if (total != issue.Fields.Total)
            {
                try
                {
                    // Fills in the number in the Jira issue.
                    await Connector.PutRequestAsync(ISSUE_BY_ID_PATH + issue.Key, String.Format(TOTAL_FIELD_CONTENT, total.ToString(CultureInfo.InvariantCulture)));
                }
                catch (Exception ex)
                {
                    return new Tuple<int, string>(0, $"{issue.Key}: {ex.Message}");
                }

                return new Tuple<int, string>(1, issue.Key);
            }
            return new Tuple<int, string>(2, issue.Key);
        }

        /// <summary>
        /// Retrieves issues within the specified Jira sprint.
        /// </summary>
        /// <returns>List of sprint issues.</returns>
        private async Task<JiraSeachResults> GetUnclosedSprintIssuesAsync()
        {
            JiraSeachResults results = new JiraSeachResults
            {
                Results = new List<JiraIssue>()
            };

            int batchSize = JIRA_PAGE_RESULTS_REST_LIMIT;
            int start = 0;

            // Loops through batches of the REST results.
            while (batchSize >= JIRA_PAGE_RESULTS_REST_LIMIT)
            {
                // Prepares the resource path of the GET request retrieving the existing issues
                string resourcePath = String.Format(JQL_FILTER_PATH, SprintId, JIRA_PAGE_RESULTS_REST_LIMIT, start, FIELDS_IDS);

                string responseContent;

                try
                {
                    // Gets the JSON content from the GET request.
                    responseContent = await Connector.GetRequestAsync(resourcePath);
                }
                catch (NullReferenceException ex)
                {
                    Writer.WriteLine("Exception: Retrieving of the JIRA issues failed ({0}).", ex.Message);
                    return null;
                }
                catch (AtlassianGetRequestException ex)
                {
                    Writer.WriteLine("Exception: Retrieving of the JIRA issues failed ({0}: {1} - {2}).", ex.Message, ex.StatusCode, ex.ReasonPhrase);
                    Writer.WriteLine(ex.ResponseContent);
                    return null;
                }

                // Deserializes the request data into an object.
                JiraSeachResults batchResults = JsonConvert.DeserializeObject<JiraSeachResults>(responseContent);

                // Checks whether another result page was found.
                if (batchResults.Results.Count > 0)
                {
                    results.Results.AddRange(batchResults.Results);
                    results.Count += batchResults.Results.Count;
                }

                start += batchResults.Results.Count;
                batchSize = batchResults.Results.Count;
            }

            // Returns the parent page's child pages.
            return results;
        }
    }
}
