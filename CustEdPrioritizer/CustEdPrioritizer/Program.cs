using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CustEdPrioritizer
{
    public class Program
    {
        private static readonly string[] requiredAppSettingsBasic = { "LogFileName" };
        private static readonly string[] requiredAppSettingsConnection = { "RestBaseServiceUrl", "JiraUsername", "JiraUserApiKey" };
        private static readonly string[] requiredAppSettingsPrioritization = { "SprintId", "EmailFrom", "EmailTo", "EmailFromPassword" };
        private static readonly string[] AppSettingsEncrypted = { "JiraUserApiKey", "EmailFromPassword" };

        /// <summary>
        /// The main method launched after starting the program.
        /// The program retrieves issues from a Jira sprint and fills in fields to prioritize them according to the defined settings.
        /// </summary>
        /// <param name="args">Arguments from the command line. None are accepted.</param>
        static void Main(string[] args)
        {
            string logPath = PrepareLogPath();
            MirrorConsole(logPath);

            // Writer for both a log file and the console output.
            using (FileAndConsoleWriter writer = new FileAndConsoleWriter(logPath))
            {
                if (writer.Stream == null || writer.Writer == null)
                {
                    Console.ReadLine();
                    Environment.Exit(1);
                }

                writer.WriteLine("The log file is located at: {0}", logPath);
                
                try
                {
                    // Continues with further processing of the Jira issues.
                    ProcessIssuesAsync(writer).Wait();
                }
                catch (Exception ex)
                {
                    // Writes the exception message to both the console output and log file.
                    writer.WriteLine(ex.Message);
                    writer.WriteLine(ex.StackTrace);

                    Environment.Exit(1);
                }
            }
        }

        /// <summary>
        /// Prepares the path and filename of the output log file. The filename contains a suffix with the current date and time for better clarity.
        /// Requires the LogFileName app.config key that serves as the main part of the log file name.
        /// </summary>
        /// <returns>The full file name incl. its folder path of the output log file.</returns>
        static string PrepareLogPath()
        {
            // Gets the folder path of the output log file.
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Cannot localize the current directory path. Press a key to terminate the application.");
                Console.ReadLine();
                Environment.Exit(1);
            }
            if (String.IsNullOrEmpty(ConfigurationManager.AppSettings["LogFileName"]))
            {
                Console.WriteLine("The app.config key for a log file name is missing. Press a key to terminate the application.");
                Console.ReadLine();
                Environment.Exit(1);
            }
            
            // Returns the full file name incl. its folder path of the output log file.
            return Path.Combine(folderPath, $"{ConfigurationManager.AppSettings["LogFileName"]}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm")}.txt");
        }

        static void MirrorConsole(string path)
        {
            Trace.Listeners.Clear();

            TextWriterTraceListener twtl = new TextWriterTraceListener(path)
            {
                Name = "log",
                TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime
            };

            ConsoleTraceListener ctl = new ConsoleTraceListener(false)
            {
                TraceOutputOptions = TraceOptions.DateTime
            };

            Trace.Listeners.Add(twtl);
            Trace.Listeners.Add(ctl);
            Trace.AutoFlush = true;

            return;
        }
        
        /// <summary>
        /// Processes retrieving of Jira issues based on the given parametes and the consecutive filling in the values according to the specified formula.
        /// </summary>
        /// <param name="writer">Instance of the log writer.</param>
        static async Task ProcessIssuesAsync(FileAndConsoleWriter writer)
        {
            string[] requiredAppSettings = requiredAppSettingsBasic.Union(requiredAppSettingsConnection).Union(requiredAppSettingsPrioritization).ToArray();
            Dictionary<string, string> appSettings = new Dictionary<string, string>();

            foreach (string requiredAppSettingKey in requiredAppSettings)
            {
                string requiredAppSettingValue = ConfigurationManager.AppSettings[requiredAppSettingKey];

                if (String.IsNullOrEmpty(requiredAppSettingValue))
                {
                    writer.WriteLine($"The required {requiredAppSettingKey} app.config key is missing. Terminating the prioritization.");
                    return;
                }
                else
                {
                    appSettings.Add(requiredAppSettingKey, requiredAppSettingValue);
                }
            }
                        
            // Creates a connection with the REST services of the Atlassian products and signs in.
            AtlassianConnector connector = new AtlassianConnector(new Uri(appSettings["RestBaseServiceUrl"]), new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{appSettings["JiraUsername"]}:{appSettings["JiraUserApiKey"]}"))));

            // Converts sprint ID to a number.
            if (!Int32.TryParse(appSettings["SprintIdString"], out int sprintId))
            {
                Console.WriteLine("The sprint ID is not a number.");
                Console.ReadLine();
                Environment.Exit(1);
            }
            
            // Initializes a manager that prioritizes the backlog.
            JiraManager manager = new JiraManager(connector, writer, sprintId, appSettings["EmailFrom"], appSettings["EmailTo"], appSettings["emailFromPassword"]);

            // Prioritizes the backlog.
            await manager.PrioritizeBacklogAsync();
        }
    }
}
