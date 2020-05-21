using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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
                Console.WriteLine("C-Fuzz v0.1");
                Console.WriteLine("Usage: c-fuzz [endpoint URI]");
                Console.WriteLine("Placeholder characters:");
                Console.WriteLine("#: Placeholder to test. (http://example.com/test?id=#)");
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
                string method;
                try
                {
                    method = args[1].ToUpper();
                }
                catch (IndexOutOfRangeException)
                {
                    method = "GET";
                }
                if (method != "GET"/* && method != "POST"*/)
                {
                    Console.WriteLine("Unsupported HTTP method. See 'c-fuzz -h' for more information.");
                }
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
                await run_tests(endpoints, method);
            }
            // Terminate with success code.
            Environment.Exit(0);
        }

        private static async Task run_tests(List<string> uri_list, string method)
        {
            for (int i = 0; i < uri_list.Count; i++)
            {
                await test_endpoint(i + 1, uri_list[i], method);
            }
        }

        private static async Task test_endpoint(int test_number, string uri, string method)
        {
            HttpResponseMessage result;
            try
            {
                //if (method == "GET")
                //{
                Task<HttpResponseMessage> runner = client.GetAsync(uri);
                //}
                //else
                //{
                //    Task<HttpResponseMessage> runner = client.PostAsync(uri, new HttpContent());
                //}
                result = await runner;
            }
            catch (HttpRequestException e)
            {
                report_failure(test_number, "---", uri, method, e.Message);
                return;
            }
            if (result.IsSuccessStatusCode) report_success(test_number, result.StatusCode.ToString(), uri, method);
            else
            {
                // Output failure code with body of response.
                report_failure(test_number, result.StatusCode.ToString(), uri, method, await result.Content.ReadAsStringAsync());
            }
        }

        private static void report_success(int test_number, string response_code, string uri, string method)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Test {0}: {1} [{2}] {3}", test_number, response_code, method, uri);
            Console.ResetColor();
        }

        private static void report_failure(int test_number, string response_code, string uri, string method, string response)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Test {0}: {1} [{2}] {3}\nResponse: {4}", test_number, response_code, method, uri, response);
            Console.ResetColor();
        }

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

        private static string replace_with(string input, int index, string replacement)
        {
            return input.Insert(index, replacement).Remove(index + replacement.Length, 1);
        }
    }
}