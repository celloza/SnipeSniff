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

using Quartz;
using Serilog;
using SnipeSharp;
using SnipeSniff.Service.Descriptors;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SnipeSniff.Service
{
    public class SnipeSniffJob : IJob
    {
        /// <summary>
        /// Run the job.
        /// </summary>
        public Task Execute(IJobExecutionContext context)
        {
            serverMode = context.JobDetail.JobDataMap.GetBoolean("ServerMode");
            snipeApiUrl = context.JobDetail.JobDataMap.GetString("SnipeApiAddress");
            snipeApiToken = context.JobDetail.JobDataMap.GetString("SnipeApiToken");
            subnetString = context.JobDetail.JobDataMap.GetString("SubnetString");

            if (string.IsNullOrEmpty(snipeApiUrl))
                throw new ArgumentException("Invalid SnipeApiAddress specified.");

            if (string.IsNullOrEmpty(snipeApiToken))
                throw new ArgumentException("Invalid SnipeApiToken specified.");

            try
            {
                if (serverMode)
                {
                    this.SyncScannerDetails();
                }
                else
                {
                    this.SyncLocalDetails();
                }
            }
            catch
            {
                // Continue
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Synchronizes the local systems details with the configured <c>Snipe.It</c> 
        /// system.
        /// </summary>
        private void SyncLocalDetails()
        {
            this.SyncHostNameDetails("localhost");
        }

        /// <summary>
        /// Scans the local network and synchronizes the located systems details with the 
        /// configured <c>Snipe.It</c> system.
        /// </summary>
        private void SyncScannerDetails()
        {
            var subnetsToScan =
                subnetString
                    .Split("|".ToArray(), StringSplitOptions.RemoveEmptyEntries);

            var devices =
                    IPScanner
                    .Scan(subnetsToScan)
                    .Where(i => i.Status == System.Net.NetworkInformation.IPStatus.Success)
                    .ToList();

            foreach (var item in devices)
            {
                this.SyncHostNameDetails(item.HostName);
            }
        }

        /// <summary>
        /// Synchronizes the specified system's details with the configured <c>Snipe.It</c> 
        /// system.
        /// </summary>
        private void SyncHostNameDetails(string hostName)
        {
            // Single Device.
            SnipeItApi snipe =
                SnipeApiExtensions.CreateClient(
                    snipeApiUrl,
                    snipeApiToken);

            if (!String.IsNullOrWhiteSpace(hostName))
            {
                Log.Information($"Retrieving asset details for {hostName}");
                try
                {
                    var asset = AssetDescriptor.Create(hostName);
                    var components = ComponentDescriptor.Create(hostName);
                    try
                    {
                        Log.Information($"Synchronizing asset details for {hostName}");
                        // The current version of the SnipeSharp API has mapping issues causing the response not de serializing.
                        snipe.SyncAssetWithCompoments(asset, components);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to sync asset details for {hostName}");
                        Log.Error(ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to retrieve asset details for {hostName}");
                    Log.Error(ex.ToString());
                }
            }
        }        

        #region Instance Fields

        /// <summary>
        /// Defines whether the job is running in server mode (i.e. doing a scan on the supplied 
        /// subnets) or in agent mode (i.e. only reporting the machine its running on).
        /// </summary>
        private bool serverMode;

        /// <summary>
        /// Defines the URL to the Snipe-IT API.
        /// </summary>
        private string snipeApiUrl;

        /// <summary>
        /// Defines the access token for the Snipe-IT API.
        /// </summary>
        private string snipeApiToken;

        /// <summary>
        /// Defines the subnets to be scanned should the job be running in server mode.
        /// </summary>
        private string subnetString;

        #endregion
    }
}
