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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace SnipeSniff.Service
{
    /// <summary>
    /// <c>IPScanner</c> scans the local network for active IP addresses.
    /// </summary>
    public static class IPScanner
    {
        #region Class Methods

        /// <summary>
        /// Scans the through the specified collection of network subnets for active IP addresses.
        /// </summary>
        /// <param name="subnet">
        ///     The collection of subnets (192.168.1 | 192.168.2) that to scan.
        /// </param>
        /// <returns>
        ///     The network scan results.
        /// </returns>
        public static IEnumerable<IPScannerResult>  Scan(string[] subnet)
        {
            ConcurrentBag<IPScannerResult> results = new ConcurrentBag<IPScannerResult>();
            var ipAddresses = subnet.SelectMany(s => Enumerable.Range(1, 255).Select(i => s + "." + i));
            Parallel.ForEach(ipAddresses, new ParallelOptions() { MaxDegreeOfParallelism = 5 }, i =>
            {
                var result = IPScanner.ScanAddress(i);
                if (result != null)
                {
                    results.Add(result);
                }
            });
            return results.ToList();
        }

        /// <summary>
        /// Verifies if the specified IP address is active.
        /// </summary>
        /// <param name="address">
        ///     The IP address to verify.
        /// </param>
        /// <returns>
        ///     The <see cref="IPScannerResult"/>.
        /// </returns>
        public static IPScannerResult ScanAddress(string address)
        {
            IPScannerResult result = new IPScannerResult() { IpAddress = address };

            Ping ping = new Ping();
            PingReply pingReply = ping.Send(address);
            result.Status = pingReply.Status;

            if (pingReply.Status == IPStatus.Success)
            {
                try
                {
                    var host = Dns.GetHostEntry(address);
                    result.HostName = host.HostName;
                }
                catch
                { // Continue. 
                }
            }
            
            Console.WriteLine($"{result.IpAddress} {result.HostName} - {result.Status}");
            
            return result;
        }

        #endregion
    }

    /// <summary>
    /// <c></c> represents a <see cref="IPScanner"/> result details.
    /// </summary>
    public class IPScannerResult
    {
        #region Instance Properties        

        /// <summary>
        /// Gets or sets the <see cref="IPStatus"/> associated with the current result..
        /// </summary>
        public IPStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the IP address associated with the current result.
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the network host-name associated with the current result.
        /// </summary>
        public string HostName { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="IPScannerResult"/>.
        /// </summary>
        public IPScannerResult()
        {

        }

        #endregion
    }
}
