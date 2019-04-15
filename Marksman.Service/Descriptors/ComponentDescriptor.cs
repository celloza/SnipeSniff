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

using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SnipeSniff.Service.Descriptors
{
    /// <summary>
    /// <c>ComponentDescriptor</c> utilizes <c>Microsoft.Management.Infrastructure</c> to 
    /// describe a network accessible asset's system components.
    /// </summary>
    internal sealed class ComponentDescriptor
    {
        #region Instance Properties

        /// <summary>
        /// Gets or sets the name or description of the component.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the unique serial number for the component.
        /// </summary>
        public string Serial { get; set; }

        /// <summary>
        /// Gets or sets the name of the component manufacturers.
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets the name of the component model.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Gets or set the <see cref="DescriptorCategories"/> for the current component.
        /// </summary>
        public DescriptorCategories Category { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new default <see cref="ComponentDescriptor"/>.
        /// </summary>
        private ComponentDescriptor()
        {

        }

        #endregion

        #region Class Methods

        /// <summary>
        /// Creates a new collection of <see cref="ComponentDescriptor"/> that describes the specified 
        /// components installed in system associated with a network host-name. 
        /// </summary>
        /// <param name="hostName">
        ///     The network host-name that should be described.
        /// </param>
        /// <returns>
        ///     The resulting collection of <see cref="ComponentDescriptor"/> if successful.
        /// </returns>
        public static IEnumerable<ComponentDescriptor> Create(string hostName)
        {
            // Validate parameters.
            if (String.IsNullOrWhiteSpace(hostName))
            {
                throw new ArgumentNullException("hostName");
            }

            CimSessionOptions sessionOptions = new CimSessionOptions() { };
            sessionOptions.AddDestinationCredentials(new CimCredential(ImpersonatedAuthenticationMechanism.Negotiate));
            CimSession session = CimSession.Create(hostName, sessionOptions);

            return
                ComponentDescriptor.GetProcessors(session)
                .Concat(ComponentDescriptor.GetMemory(session))
                .Concat(ComponentDescriptor.GetHardDrives(session))
                .ToList();
        }

        /// <summary>
        /// Creates a new collection of <see cref="ComponentDescriptor"/> that describes 
        /// the physical hard drives installed in the system associated <see cref="CimSession"/>. 
        /// </summary>
        /// <param name="session">
        ///     A <see cref="CimSession session"/> used to access system details.
        /// </param>
        /// <returns>
        ///     The resulting collection of hard drive <see cref="ComponentDescriptor"/>s found.
        /// </returns>
        private static IEnumerable<ComponentDescriptor> GetHardDrives(CimSession session)
        {
            // Validate parameters.
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }


            IEnumerable<CimInstance> hddDetails = session.EnumerateInstances(@"root\cimv2", "Win32_DiskDrive");

            var results = 
                hddDetails
                .Where(item => Convert.ToString(item.CimInstanceProperties["Name"].Value, CultureInfo.InvariantCulture).Contains("PHYSICAL"))
                .Select(item => new ComponentDescriptor()
                {
                    Name =
                        String.Join(" ",
                            Convert.ToString(item.CimInstanceProperties["Model"].Value, CultureInfo.InvariantCulture),
                            Convert.ToInt32((((Convert.ToInt64(item.CimInstanceProperties["Size"].Value) / 1024f) / 1024f) / 1024f)) + "Gb"),
                    Manufacturer = Convert.ToString(item.CimInstanceProperties["Manufacturer"].Value, CultureInfo.InvariantCulture).Trim(),
                    Serial = Convert.ToString(item.CimInstanceProperties["SerialNumber"].Value, CultureInfo.InvariantCulture).Trim(),                    
                    Model = Convert.ToString(item.CimInstanceProperties["Model"].Value, CultureInfo.InvariantCulture).Trim(),
                    Category = DescriptorCategories.HardDrive
                });

            return results.ToList();
        }

        /// <summary>
        /// Creates a new collection of <see cref="ComponentDescriptor"/> that describes 
        /// the physical memory modules installed in the system associated <see cref="CimSession"/>. 
        /// </summary>
        /// <param name="session">
        ///     A <see cref="CimSession session"/> used to access system details.
        /// </param>
        /// <returns>
        ///     The resulting collection of memory modules <see cref="ComponentDescriptor"/>s found.
        /// </returns>
        private static IEnumerable<ComponentDescriptor> GetMemory(CimSession session)
        {
            // Validate parameters.
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            IEnumerable<CimInstance> memoryDetails = session.EnumerateInstances(@"root\cimv2", "Win32_PhysicalMemory");
            
            var results =
                memoryDetails
                .Select(item => new ComponentDescriptor()
                {
                    Serial = Convert.ToString(item.CimInstanceProperties["SerialNumber"].Value, CultureInfo.InvariantCulture),
                    Name =                        
                        String.Join(" ",
                         Convert.ToString(item.CimInstanceProperties["Manufacturer"].Value, CultureInfo.InvariantCulture).Trim(),
                         Convert.ToString(item.CimInstanceProperties["PartNumber"].Value, CultureInfo.InvariantCulture).Trim(),
                         Convert.ToString(item.CimInstanceProperties["ConfiguredClockSpeed"].Value, CultureInfo.InvariantCulture).Trim(),
                         (((Convert.ToInt64(item.CimInstanceProperties["Capacity"].Value) / 1024f) / 1024f) / 1024f) + "Gb"),
                    Manufacturer = Convert.ToString(item.CimInstanceProperties["Manufacturer"].Value, CultureInfo.InvariantCulture),
                    Model = Convert.ToString(item.CimInstanceProperties["PartNumber"].Value, CultureInfo.InvariantCulture).Trim(),
                    Category = DescriptorCategories.Memory
                });            

            return results.ToList();
        }

        /// <summary>
        /// Creates a new collection of <see cref="ComponentDescriptor"/> that describes 
        /// the physical processors installed in the system associated <see cref="CimSession"/>. 
        /// </summary>
        /// <param name="session">
        ///     A <see cref="CimSession session"/> used to access system details.
        /// </param>
        /// <returns>
        ///     The resulting collection of processors <see cref="ComponentDescriptor"/>s found.
        /// </returns>
        private static IEnumerable<ComponentDescriptor> GetProcessors(CimSession session)
        {
            // Validate parameters.
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            IEnumerable<CimInstance> processorDetails = session.EnumerateInstances(@"root\cimv2", "Win32_Processor");

            var results =
               processorDetails
                .Where(item => Convert.ToString(item.CimInstanceProperties["ProcessorType"].Value, CultureInfo.InvariantCulture).Contains("3"))
               .Select(item => new ComponentDescriptor()
               {
                   Serial = Convert.ToString(item.CimInstanceProperties["ProcessorId"].Value, CultureInfo.InvariantCulture),
                   Name = Convert.ToString(item.CimInstanceProperties["Name"].Value, CultureInfo.InvariantCulture),
                   Manufacturer = Convert.ToString(item.CimInstanceProperties["Manufacturer"].Value, CultureInfo.InvariantCulture),
                   Model = Convert.ToString(item.CimInstanceProperties["Name"].Value, CultureInfo.InvariantCulture),
                   Category = DescriptorCategories.Processor
               });

            return results.ToList();
        }

        #endregion
    }
}
