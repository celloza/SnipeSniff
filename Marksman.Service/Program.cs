/*
 * Copyright 2019 marksman Contributors (https://github.com/Scope-IT/marksman)
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
            var intervalSeconds = int.Parse(ConfigurationManager
                               .AppSettings["SnipeSniff:ServiceInterval"]);

            bool serverMode = bool.Parse(ConfigurationManager
                              .AppSettings["SnipeSniff:ServerMode"]);

            string snipeApiAddress = ConfigurationManager
                              .AppSettings["Snipe:ApiAddress"];

            string snipeApiToken = ConfigurationManager
                              .AppSettings["Snipe:ApiToken"];

            string subnetString = ConfigurationManager
                              .AppSettings["SnipeSniff:ScannerSubnet"];


            // Setup and start the SnipeSniffService
            var rc = HostFactory.Run(x =>
            {
                x.Service<SnipeSniffService>(s =>
                {
                    s.ConstructUsing(name => new SnipeSniffService(intervalSeconds, 
                                                                   serverMode, 
                                                                   snipeApiAddress, 
                                                                   snipeApiToken,
                                                                   subnetString));
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                // Configure logging
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.File($"{Path.GetTempPath()}logs\\myapp.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                //x.UseSerilog(Log.Logger);                

                x.SetDescription("Periodically updates the configured Snipe-IT server with the details from this machine.");
                x.SetDisplayName("SnipeSniff");
                x.SetServiceName("SnipeSniff");
            });

            

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;
        }
    }
}