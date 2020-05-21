using System;
using System.Collections.Generic;

namespace c_fuzz
{
    class Program
    {
        static void Main(string[] args)
        {
            // Check for help flags or no options at all.
            if(args.Length == 0 || args[0] == "-h" || args[0] == "--help")
            {
                Console.WriteLine("C-Fuzz v0.1");
                Console.WriteLine("Usage: c-fuzz [endpoint URI] [POST|GET]");
            }
            // Check for version flags.
            else if(args[0] == "-v" || args[0] == "--version")
            {
                Console.WriteLine("0.1");
            }
            // Else trigger main code.
            else
            {
                string uri = args[0];
                if(!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                {
                    Console.WriteLine("ERROR: No URI provided. See 'c-fuzz -h' for more information.");
                    Environment.Exit(1);
                }
                List<string> int_endpoints;
                while (uri.IndexOf('%') != -1)
                {
                    int int_count = uri.IndexOf('%');
                    
                }
            }
            // Terminate with success code.
            Environment.Exit(0);
        }
    }
}
