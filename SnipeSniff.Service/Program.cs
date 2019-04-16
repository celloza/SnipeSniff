/*
 * Copyright 2019 SnipeSniff Contributors (https://github.com/grimstoner/SnipeSniff)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Serilog;
using System;
using System.Configuration;
using System.IO;
using Topshelf;

namespace SnipeSniff
{
    /// <summary>
    /// <c>Program</c> defines the Marksman Service application entry point.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main execution entry. 
        /// </summary>
        public static void Main()
        {
            // Check if all configuration settings are good
            bool logToFile;
            if (!bool.TryParse(ConfigurationManager.AppSettings["SnipeSniff:LogToFile"], out logToFile))
            {
                Console.WriteLine("The LogToFile parameter could not be parsed. Logging to file is disabled.");
                logToFile = false;
            }

            string logFileLocation = ConfigurationManager.AppSettings["Snipe:LogFileLocation"];
            if (logFileLocation is null || logFileLocation.Length == 0)
            {
                Console.WriteLine("The LogFileLocation parameter is not valid. ");
                logFileLocation = $"{Path.GetTempPath()}logs\\SnipeSniff.log";
            }
            else
            {
                if (!Directory.Exists(logFileLocation))
                {
                    Console.WriteLine("The LogFileLocation refers to a folder that does not exist.");
                    logToFile = false;
                }
            }

            if (logToFile)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.File($"{Path.GetTempPath()}logs\\myapp.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();
            }
            else
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .CreateLogger();
            }
            
            int intervalSeconds;
            if (!int.TryParse(ConfigurationManager.AppSettings["SnipeSniff:ServiceInterval"], out intervalSeconds))
            {
                Log.Fatal("The ServiceInterval parameter could not be parsed.");
            }

            bool serverMode;
            if (!bool.TryParse(ConfigurationManager.AppSettings["SnipeSniff:ServerMode"], out serverMode))
            {
                Log.Fatal("The ServerMode parameter could not be parsed.");
            }

            Uri snipeApiAddress;
            if (!Uri.TryCreate(ConfigurationManager.AppSettings["Snipe:ApiAddress"], UriKind.Absolute, out snipeApiAddress))
            {
                Log.Fatal("The ApiAddress parameter is not a valid URL.");
            }

            string snipeApiToken = ConfigurationManager.AppSettings["Snipe:ApiToken"];
            if (snipeApiToken is null || snipeApiToken.Length == 0)
            {
                Log.Fatal("The ApiToken parameter is not valid.");
            }

            string subnetString = ConfigurationManager.AppSettings["SnipeSniff:ScannerSubnet"];
            
            // Setup and start the SnipeSniffService
            var rc = HostFactory.Run(x =>
            {
                x.Service<SnipeSniffService>(s =>
                {
                    s.ConstructUsing(name => new SnipeSniffService(intervalSeconds,
                                                                   serverMode,
                                                                   snipeApiAddress.ToString(),
                                                                   snipeApiToken,
                                                                   subnetString));
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Periodically updates the configured Snipe-IT server with the details from this machine.");
                x.SetDisplayName("SnipeSniff");
                x.SetServiceName("SnipeSniff");
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;
        }
    }
}