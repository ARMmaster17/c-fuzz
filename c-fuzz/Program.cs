using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

namespace c_fuzz
{
    internal class Program
    {
        private static readonly HttpClient client = new HttpClient();

        private static async Task Main(string[] args)
        {
            // Check for help flags or no options at all.
            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
            {
                Console.WriteLine("C-Fuzz v0.2");
                Console.WriteLine("Usage: c-fuzz [endpoint URI]");
                Console.WriteLine("Placeholder characters:");
                Console.WriteLine("#: Placeholder to test. (http://example.com/test?id=#)");
                Console.WriteLine("-l or --log: Writes output to text file in current working directory.");
            }
            // Check for version flags.
            else if (args[0] == "-v" || args[0] == "--version")
            {
                Console.WriteLine("0.1");
            }
            // Else trigger main code.
            else
            {
                string uri = args[0];
                bool logOutput = false;
                // Check if log flag is passed. If it is, we also need to log output to a text file
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-l" || args[i] == "--log")
                    {
                        logOutput = true;
                        File.WriteAllText(Directory.GetCurrentDirectory() + "\\result.txt", String.Format("{0} {1} UTC\nInput: {2}\n", DateTime.UtcNow.ToLongDateString(), DateTime.UtcNow.ToLongTimeString(), uri));
                        break;
                    }
                }
                string method = "GET";

                //if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                //{
                //    Console.WriteLine("ERROR: No URI provided. See 'c-fuzz -h' for more information.");
                //    Environment.Exit(1);
                //}
                List<string> endpoints = new List<string>();
                endpoints.Add(uri);
                for (int i = 0; i < uri.Length; i++)
                {
                    // Check for placeholder.
                    if (uri[i] == '#')
                    {
                        generate_permutations(ref endpoints);
                    }
                    // Else, normal character detected. Continue to next character.
                }
                // If no special characters are detected, send the URI through to testing.
                if (endpoints.Count == 0) endpoints.Add(uri);
                await run_tests(endpoints, method, logOutput);
                if (logOutput) Console.WriteLine("Log file written to {0}.", Directory.GetCurrentDirectory() + "\\result.txt");
            }
            // Terminate with success code.
            Environment.Exit(0);
        }

        /// <summary>
        /// Iterates over generated permutations and handles test running.
        /// </summary>
        /// <param name="uri_list">List of URI permutations with no wildcards.</param>
        /// <param name="method">HTTP method to use (only GET is supported at the moment.</param>
        /// <param name="logOutput">Determines if output will also be written to result file along with STDOUT.</param>
        /// <returns>Task<> object to monitor async operations.</returns>
        private static async Task run_tests(List<string> uri_list, string method, bool logOutput)
        {
            for (int i = 0; i < uri_list.Count; i++)
            {
                await test_endpoint(i + 1, uri_list[i], method, logOutput);
            }
        }

        /// <summary>
        /// Entry point for testing a single URI. Handles error/success output to STDOUT and logging if enabled.
        /// </summary>
        /// <param name="test_number">UID of test number within master URI list.</param>
        /// <param name="uri">URI to test.</param>
        /// <param name="method">HTTP method to use (only GET is supported at the moment).</param>
        /// <param name="logOutput">Determines if the output will also be written to the result file along with STDOUT.</param>
        /// <returns>Task<> object to monitor async operations.</returns>
        private static async Task test_endpoint(int test_number, string uri, string method, bool logOutput)
        {
            HttpResponseMessage result;
            try
            {
                Task<HttpResponseMessage> runner = client.GetAsync(uri);
                result = await runner;
            }
            catch (HttpRequestException e)
            {
                report_failure(test_number, "---", uri, method, e.Message, logOutput);
                return;
            }
            if (result.IsSuccessStatusCode) report_success(test_number, result.StatusCode.ToString(), uri, method, logOutput);
            else
            {
                // Output failure code with body of response.
                report_failure(test_number, result.StatusCode.ToString(), uri, method, await result.Content.ReadAsStringAsync(), logOutput);
            }
        }

        /// <summary>
        /// Wrapper method to output a success message to STDOUT and the log if enabled.
        /// </summary>
        /// <param name="test_number">UID of test number within master URI list.</param>
        /// <param name="response_code">Response code returned by HTTP request.</param>
        /// <param name="uri">URI to test.</param>
        /// <param name="method">HTTP method to use (only GET is supported at the moment).</param>
        /// <param name="logOutput">Determines if the output will also be written to the result file along with STDOUT.</param>
        private static void report_success(int test_number, string response_code, string uri, string method, bool logOutput)
        {
            report(ConsoleColor.Green, test_number, response_code, uri, method, "", logOutput);
        }

        /// <summary>
        /// Wrapper method to output a success message to STDOUT and the log if enabled.
        /// </summary>
        /// <param name="test_number">UID of test number within master URI list.</param>
        /// <param name="response_code">Response code returned by HTTP request.</param>
        /// <param name="uri">URI to test.</param>
        /// <param name="method">HTTP method to use (only GET is supported at the moment).</param>
        /// <param name="response">Response body from HTTP request.</param>
        /// <param name="logOutput">Determines if the output will also be written to the result file along with STDOUT.</param>
        private static void report_failure(int test_number, string response_code, string uri, string method, string response, bool logOutput)
        {
            report(ConsoleColor.Red, test_number, response_code, uri, method, response, logOutput);
        }

        /// <summary>
        /// Base method to send messages to STDOUT and the log if enabled.
        /// </summary>
        /// <param name="color">Color to use for output to STDOUT.</param>
        /// <param name="test_number">UID of test number within master URI list.</param>
        /// <param name="response_code">Response code returned by HTTP request.</param>
        /// <param name="uri">URI to test.</param>
        /// <param name="method">HTTP method to use (only GET is supported at the moment).</param>
        /// <param name="response">Response body from HTTP request.</param>
        /// <param name="logOutput">Determines if the output will also be written to the result file along with STDOUT.</param>
        private static void report(ConsoleColor color, int test_number, string response_code, string uri, string method, string response, bool logOutput)
        {
            Console.ForegroundColor = color;
            StreamWriter sw;

            string actionLine = String.Format("Test {0}: {1} [{2}] {3}", test_number, response_code, method, uri);
            string responseLine = String.Format("Response: {0}", response);

            if (logOutput)
            {
                sw = File.AppendText(Directory.GetCurrentDirectory() + "\\result.txt");
                sw.WriteLine(actionLine);
                if (response != "")
                {
                    sw.WriteLine(responseLine);
                }
                sw.Flush();
                sw.Close();
            }

            Console.WriteLine(actionLine);
            if (response != "")
            {
                Console.WriteLine(responseLine);
            }

            Console.ResetColor();
        }

        /// <summary>
        /// Takes given URI and generates list of URIs with randomized input values.
        /// </summary>
        /// <param name="master_list">List<> object with base URI to expand.</param>
        private static void generate_permutations(ref List<string> master_list)
        {
            List<string> new_permutations = new List<string>();
            foreach (string item in master_list)
            {
                int index = item.IndexOf('#');
                // Add all integer placeholders.
                new_permutations.Add(replace_with(item, index, "1"));
                new_permutations.Add(replace_with(item, index, "0"));
                new_permutations.Add(replace_with(item, index, "-1"));
                new_permutations.Add(replace_with(item, index, Int32.MaxValue.ToString()));
                new_permutations.Add(replace_with(item, index, "a"));
                new_permutations.Add(replace_with(item, index, "+"));
                new_permutations.Add(replace_with(item, index, "\"fskjnjsdjlk\""));
                new_permutations.Add(replace_with(item, index, ""));
                new_permutations.Add(replace_with(item, index, "\"\""));
            }
            master_list = new_permutations;
        }

        /// <summary>
        /// Helper method that replaces given index of a character with an entire string.
        /// </summary>
        /// <param name="input">Original string.</param>
        /// <param name="index">Index of character to replace.</param>
        /// <param name="replacement">String to insert in place of referenced character.</param>
        /// <returns></returns>
        private static string replace_with(string input, int index, string replacement)
        {
            return input.Insert(index, replacement).Remove(index + replacement.Length, 1);
        }
    }
}