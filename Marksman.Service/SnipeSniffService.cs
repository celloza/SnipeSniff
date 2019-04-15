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

using SnipeSniff.Service;
using SnipeSniff.Service.Descriptors;
using SnipeSharp;
using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace SnipeSniff
{
    /// <summary>
    /// Locates and synchronizes system details with the configured <c>Snipe.It</c> 
    /// system. 
    /// </summary>
    public class SnipeSniffService : IDisposable
    {        
        /// <summary>
        /// 
        /// </summary>
        public SnipeSniffService()
        {
            // Init the scheduler here

        }

        #region IDisposable Implementation

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    if (this.timer != null)
                    {
                        this.timer.Stop();
                        this.timer.Elapsed -= this.OnTimerElapsed;
                        this.timer.Dispose();
                        this.timer = null;
                    }
                }

                this.isDisposed = true;
            }
        }

        #endregion

        /// <summary>
        /// Asynchronously starts the current service.
        /// </summary>
        /// <returns>
        ///     True if the service was started successfully.
        /// </returns>
        public bool StartAsync()
        {
            Task.Run(() =>
            {
                this.Start();
            });
            return true;
        }

        /// <summary>
        /// Starts the current service.
        /// </summary>
        private void Start()
        {
            var intervalString = ConfigurationManager.AppSettings["SnipeSniff:ServiceInterval"];

            if (intervalString is null)
                throw new ConfigurationErrorsException("The ServiceInterval setting is not defined.");

            var interval =
                TimeSpan.Parse(
                    intervalString,
                    CultureInfo.InvariantCulture).TotalMilliseconds;
            
            this.timer = 
                new Timer()
                {
                    Interval = interval > 0 ? interval : TimeSpan.FromHours(24).TotalMilliseconds,
                    AutoReset = false
                };
            this.timer.Elapsed += this.OnTimerElapsed;
            this.OnTimerElapsed(this, null);
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
            var scannerSubnets = 
                ConfigurationManager
                    .AppSettings["Marksman:ScannerSubnet"]
                    .Split("|".ToArray(), StringSplitOptions.RemoveEmptyEntries);

            var devices = 
                    IPScanner
                    .Scan(scannerSubnets)
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
                    ConfigurationManager.AppSettings["Snipe:ApiAddress"],
                    ConfigurationManager.AppSettings["Snipe:ApiToken"]);

            if (!String.IsNullOrWhiteSpace(hostName))
            {
                Console.WriteLine($"Retrieving asset details for {hostName}");
                try
                {
                    var asset = AssetDescriptor.Create(hostName);
                    var components = ComponentDescriptor.Create(hostName);
                    try
                    {
                        Console.WriteLine($"Synchronizing asset details for {hostName}");
                        // The current version of the SnipeSharp API has mapping issues causing the response not de serializing.
                        snipe.SyncAssetWithCompoments(asset, components);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to sync asset details for {hostName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to retrieve asset details for {hostName}");
                }
            }
        }

        /// <summary>
        /// Stops the current service.
        /// </summary>
        public void Stop()
        {
            this.timer.Stop();
        }
       
        /// <summary>
        /// Called when the current service timer has elapsed.
        /// </summary>     
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine($"Service interval timer elapsed.");
            this.timer.Stop();
            try
            {
                // Start the scheduler here
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["SnipeSniff:ScannerEnabled"]))
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
                // Continue.
            }
            finally
            {
                Console.WriteLine($"Service interval timer started.");
                this.timer.Start();
            }
        }

        #region Instance Fields

        /// <summary>
        /// The <see cref="Timer"/> instance for the current <see cref="SnipeSniffService"/>.
        /// </summary>
        private Timer timer;

        /// <summary>
        /// Indicates if the current <see cref="SnipeSniffService"/> is disposed.
        /// </summary>
        private bool isDisposed;

        #endregion
    }
}
