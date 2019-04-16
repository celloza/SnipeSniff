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
    /// <c>AssetDescriptor</c> utilizes <c>Microsoft.Management.Infrastructure</c> to 
    /// describe a network accessible asset.
    /// </summary>
    internal sealed class AssetDescriptor
    {
        #region Instance Properties
        
        /// <summary>
        /// Gets or sets the unique serial number for the asset.
        /// </summary>
        public string Serial { get; set; }

        /// <summary>
        /// Gets or sets the name or description of the asset.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the asset manufacturers.
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets the name of the asset model.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Gets or sets the asset model number.
        /// </summary>
        public string ModelNumber { get; set; }

        /// <summary>
        /// Gets or set the <see cref="DescriptorCategories"/> for the current asset.
        /// </summary>
        public DescriptorCategories Category { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new default <see cref="AssetDescriptor"/>.
        /// </summary>
        private AssetDescriptor()
        {
        }

        #endregion

        #region Class Methods

        /// <summary>
        /// Creates a new <see cref="AssetDescriptor"/> that describes the specified 
        /// network host-name. 
        /// </summary>
        /// <param name="hostName">
        ///     The network host-name that should be described.
        /// </param>
        /// <returns>
        ///     The resulting <see cref="AssetDescriptor"/> if successful.
        /// </returns>
        public static AssetDescriptor Create(string hostName)
        {
            CimSessionOptions sessionOptions = new CimSessionOptions() { };
            sessionOptions.AddDestinationCredentials(new CimCredential(ImpersonatedAuthenticationMechanism.Negotiate));
            CimSession session = CimSession.Create(hostName, sessionOptions);

            CimInstance computerDetails = session.EnumerateInstances(@"root\cimv2", "Win32_ComputerSystem").FirstOrDefault();
            CimInstance productDetails = session.EnumerateInstances(@"root\cimv2", "Win32_ComputerSystemProduct").FirstOrDefault();

            return new AssetDescriptor()
            {
                Serial = Convert.ToString(productDetails.CimInstanceProperties["IdentifyingNumber"].Value, CultureInfo.InvariantCulture),
                Name = Convert.ToString(computerDetails.CimInstanceProperties["Name"].Value, CultureInfo.InvariantCulture),
                Manufacturer = Convert.ToString(computerDetails.CimInstanceProperties["Manufacturer"].Value, CultureInfo.InvariantCulture),
                Model = String.Join(" ", computerDetails.CimInstanceProperties["Manufacturer"].Value, computerDetails.CimInstanceProperties["Model"].Value),
                ModelNumber = Convert.ToString(computerDetails.CimInstanceProperties["Model"].Value, CultureInfo.InvariantCulture),
                Category = pcSystemTypeExMap[Convert.ToInt32(computerDetails.CimInstanceProperties["PCSystemTypeEx"].Value, CultureInfo.InvariantCulture)]
            };
        }

        #endregion

        #region Class Fields

        /// <summary>
        /// Maps the <c>Microsoft.Management.Infrastructure</c> <c>PcSystemTypeEx</c> value to an equivalent 
        /// <see cref="DescriptorCategories"/> value.
        /// </summary>
        /// <remarks >
        ///     Based on https://docs.microsoft.com/en-us/dotnet/api/microsoft.powershell.commands.pcsystemtypeex?view=powershellsdk-1.1.0
        /// </remarks>
        private static Dictionary<int, DescriptorCategories> pcSystemTypeExMap =
            new Dictionary<int, DescriptorCategories>()
            {
                // Desktop, System is a desktop
                { 0, DescriptorCategories.Desktops },
                // Desktop, System is a desktop
                { 1, DescriptorCategories.Desktops },
                // Mobile, System is a mobile device
                { 2, DescriptorCategories.Laptops },
                // Workstation, System is a workstation
                { 3, DescriptorCategories.Desktops },
                // EnterpriseServer, System is an Enterprise Server
                { 4, DescriptorCategories.Servers },
                 // SOHOServer, System is a Small Office and Home Office(SOHO) Server
                { 5, DescriptorCategories.Servers },
                 // AppliancePC, System is an appliance PC
                { 6, DescriptorCategories.Desktops },
                 // PerformanceServer, System is a performance server
                { 7, DescriptorCategories.Servers },
                 // Slate , System is a slate/tablet
                { 8, DescriptorCategories.Tablets },
                 // Maximum
                { 9, DescriptorCategories.Desktops }
            };

        #endregion
    }
}