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

using SnipeSniff.Service.Descriptors;
using SnipeSharp;
using SnipeSharp.Endpoints.Models;
using SnipeSharp.Endpoints.SearchFilters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SnipeSniff.Service
{
    /// <summary>
    /// <c>SnipeApiExtensions</c> provides extension methods used to synchronise 
    /// <c>Snipt.It</c> artefacts.
    /// </summary>
    internal static class SnipeApiExtensions
    {
        #region Class Methods

        /// <summary>
        /// Creates a new <see cref="SnipeItApi"/> instance using the specified endpoint 
        /// and credentials.
        /// </summary>
        /// <param name="apiAddress">
        ///     The <c>Snipt.It</c> web API address.
        /// </param>
        /// <param name="apiToken">
        ///     The <c>Snipt.It</c> web API authentication token.
        /// </param>
        /// <returns>
        ///     A new initialized <see cref="SnipeItApi"/> instance.
        /// </returns>
        public static SnipeItApi CreateClient(
            string apiAddress, 
            string apiToken)
        {
            // Validate parameters
            if (String.IsNullOrWhiteSpace(apiAddress))
            {
                throw new ArgumentNullException("apiAddress");
            }
            if (String.IsNullOrWhiteSpace(apiToken))
            {
                throw new ArgumentNullException("apiToken");
            }

            // Single Device.
            SnipeItApi snipe = new SnipeItApi();
            snipe.ApiSettings.BaseUrl = new Uri(apiAddress);
            snipe.ApiSettings.ApiToken = apiToken;            
            return snipe;
        }
       
        public static void SyncAssetWithCompoments(
            this SnipeItApi snipe, 
            AssetDescriptor assetDetails, 
            IEnumerable<ComponentDescriptor> components)
        {
            if (snipe == null)
            {
                throw new ArgumentNullException("snipe");
            }
            if (assetDetails == null)
            {
                throw new ArgumentNullException("assetDetails");
            }
            if (components == null)
            {
                throw new ArgumentNullException("components");
            }

            var snipeAsset = snipe.SyncAsset(assetDetails);

            //// The Snipe API does not have the capability to check-out components.
            //var snipeComponents = snipe.SyncComponents(components);
            //foreach (var item in snipeComponents)
            //{
            //    snipe.CheckOutComponent(snipeAsset.Serial, item.Serial);
            //}
        }

        /// <summary>
        /// Synchronizes the specified <see cref="AssetDescriptor"/> details with a 
        /// <c>Snipt.It</c> asset or creates one if not asset matching the specified 
        /// serial exists.
        /// </summary>
        /// <param name="snipe">
        ///     The <see cref="SnipeItApi"/> instance used to access the <c>Snipt.It</c> 
        ///     web API.
        /// </param>
        /// <param name="assetDetails">
        ///     The <see cref="AssetDescriptor"/> describing the asset's details.
        /// </param>
        /// <returns>
        ///     The resulting <c>Snipt.It</c> <see cref="Asset"/>.
        /// </returns>
        public static Asset SyncAsset(this SnipeItApi snipe, AssetDescriptor assetDetails)
        {
            // Validate parameters.
            if (snipe == null)
            {
                throw new ArgumentNullException("snipe");
            }
            if (assetDetails == null)
            {
                throw new ArgumentNullException("assetDetails");
            }

            Asset snipeAsset = snipe.AssetManager.FindOne(new SearchFilter { Search = assetDetails.Serial });
            var manufacturer = SnipeApiExtensions.SyncManufacturer(snipe, assetDetails.Manufacturer);
            string snipeCategory = SnipeApiExtensions.DefaultSnipeCategoriesMap[assetDetails.Category];
            var category = SnipeApiExtensions.SyncCategory(snipe, snipeCategory, "asset");
            var model = SnipeApiExtensions.SyncModel(snipe, manufacturer, category, assetDetails.Model, assetDetails.ModelNumber);

            if (snipeAsset == null)
            {
                snipeAsset =
                    new Asset()
                    {
                        Name = assetDetails.Name,
                        ModelNumber = assetDetails.ModelNumber,
                        Manufacturer = manufacturer,
                        Category = category,
                        Model = model
                    };

                var response = snipe.AssetManager.Create(snipeAsset);
                snipeAsset = snipe.AssetManager.FindOne(new SearchFilter { Search = assetDetails.Serial });
            }
            else
            {
                snipeAsset.Name = assetDetails.Name;
                snipeAsset.ModelNumber = assetDetails.ModelNumber;
                snipeAsset.Manufacturer = manufacturer;
                snipeAsset.Category = category;
                snipeAsset.Model = model;

                var response = snipe.AssetManager.Update(snipeAsset);
            }

            return snipeAsset;
        }

        /// <summary>
        /// Synchronizes the specified collection of <see cref="ComponentDescriptor"/> 
        /// details with a <c>Snipt.It</c> component or creates one if not asset matching 
        /// the specified serial exists.
        /// </summary>
        /// <param name="snipe">
        ///      The <see cref="SnipeItApi"/> instance used to access the <c>Snipt.It</c> 
        ///      web API.
        /// </param>
        /// <param name="components">
        ///      The collection of <see cref="ComponentDescriptor"/>s describing the 
        ///      components details.
        /// </param>
        /// <returns>
        ///     The resulting <c>Snipt.It</c> collection of <see cref="Component"/>s.
        /// </returns>
        public static IEnumerable<Component> SyncComponents(
            this SnipeItApi snipe, 
            IEnumerable<ComponentDescriptor> components)
        {
            // Validate parameters.
            if (snipe == null)
            {
                throw new ArgumentNullException("snipe");
            }           
            if (components == null)
            {
                throw new ArgumentNullException("components");
            }


            List<Component> results = new List<Component>();
            if (components.Any())
            {
                foreach (var item in components)
                {
                    results.Add(snipe.SyncComponent(item));
                }
            }

            return results;
        }

        /// <summary>
        /// Synchronizes the specified <see cref="ComponentDescriptor"/> details with a 
        /// <c>Snipt.It</c> component or creates one if no component matching the specified 
        /// serial exists.
        /// </summary>
        /// <param name="snipe">
        ///     The <see cref="SnipeItApi"/> instance used to access the <c>Snipt.It</c> 
        ///     web API.
        /// </param>
        /// <param name="component">
        ///     The <see cref="ComponentDescriptor"/> describing the component's details.
        /// </param>
        /// <returns>
        ///      The resulting <c>Snipt.It</c> <see cref="Component"/>.
        /// </returns>
        public static Component SyncComponent(this SnipeItApi snipe, ComponentDescriptor component)
        {
            // Validate parameters.
            if (snipe == null)
            {
                throw new ArgumentNullException("snipe");
            }          
            if (component == null)
            {
                throw new ArgumentNullException("component");
            }
            if(String.IsNullOrWhiteSpace(component.Serial))
            {
                throw new ArgumentNullException("component.Serial");
            }

            string snipeCategory = SnipeApiExtensions.DefaultSnipeCategoriesMap[component.Category];
            Component snipeComponent = snipe.ComponentManager.FindAll(new SearchFilter { Search = component.Serial, Limit = 1 })?.Rows.FirstOrDefault();
            var manufacturer = SnipeApiExtensions.SyncManufacturer(snipe, component.Manufacturer);
            var category = SnipeApiExtensions.SyncCategory(snipe, snipeCategory, "component");

            if(snipeComponent == null)
            {
                snipeComponent = new Component()
                {                                        
                    Category = category,
                    Name = component.Name,
                    //Serial = component.Serial,
                    Quantity = 1                    
                };
                var response = snipe.ComponentManager.Create(snipeComponent);
                snipeComponent = snipe.ComponentManager.FindAll(new SearchFilter { Search = component.Serial, Limit = 1 })?.Rows.FirstOrDefault();
            }

            return snipeComponent;                        
        }

        /// <summary>
        /// Synchronizes the specified manufacturer details with an existing 
        /// <c>Snipt.It</c> manufacturer or creates one if no manufacturer exists.
        /// </summary>
        /// <param name="snipe">
        ///     The <see cref="SnipeItApi"/> instance used to access the <c>Snipt.It</c> 
        ///     web API.
        /// </param>
        /// <param name="manufacturer">
        ///     The manufacturer name.
        /// </param>
        /// <returns>
        ///      The resulting <c>Snipt.It</c> <see cref="Manufacturer"/>.
        /// </returns>
        public static Manufacturer SyncManufacturer(this SnipeItApi snipe, string manufacturer)
        {
            // Validate parameters.
            if (snipe == null)
            {
                throw new ArgumentNullException("snipe");
            }
            if (String.IsNullOrWhiteSpace(manufacturer))
            {
                throw new ArgumentNullException("manufacturer");
            }

            Manufacturer snipeManufacturer = snipe.ManufacturerManager.FindAll(new SearchFilter { Search = manufacturer })?.Rows.FirstOrDefault();
            if (snipeManufacturer == null)
            {
                snipeManufacturer = new Manufacturer() { Name = manufacturer };
                var response = snipe.ManufacturerManager.Create(snipeManufacturer);
                snipeManufacturer = snipe.ManufacturerManager.FindAll(new SearchFilter { Search = manufacturer })?.Rows.FirstOrDefault();
            }
            return snipeManufacturer;
        }

        /// <summary>
        ///  Synchronizes the specified model details with an existing 
        /// <c>Snipt.It</c> model or creates a new one if no model exists.
        /// </summary>
        /// <param name="snipe">
        ///     The <see cref="SnipeItApi"/> instance used to access the <c>Snipt.It</c> 
        ///     web API.
        /// </param>
        /// <param name="manufacturer">
        ///     The <c>Snipt.It</c> <see cref="Manufacturer"/> that should be associated 
        ///     with the specified model.
        /// </param>
        /// <param name="category">
        ///      The <c>Snipt.It</c> <see cref="Category"/> that should be associated 
        ///     with the specified model.
        /// </param>
        /// <param name="modelName">
        ///     The model name.
        /// </param>
        /// <param name="modelNumber">
        ///     The model number.
        /// </param>
        /// <returns>
        ///     The resulting <c>Snipt.It</c> <see cref="Model"/>.
        /// </returns>
        public static Model SyncModel(
            this SnipeItApi snipe, 
            Manufacturer manufacturer, 
            Category category, 
            string modelName, 
            string modelNumber)
        {
            // Validate parameters.
            if (snipe == null)
            {
                throw new ArgumentNullException("snipe");
            }
            if (manufacturer == null)
            {
                throw new ArgumentNullException("manufacturer");
            }
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }
            if (String.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentNullException("modelName");
            }
            if (String.IsNullOrWhiteSpace(modelNumber))
            {
                throw new ArgumentNullException("modelNumber");
            }


            Model snipeModel = snipe.ModelManager.FindAll(new SearchFilter { Search = modelName }).Rows.FirstOrDefault();
            if (snipeModel == null)
            {
                snipeModel = new Model() { Name = modelName, ModelNumber = modelNumber, Manufacturer = manufacturer, Category = category };
                var response = snipe.ModelManager.Create(snipeModel);
                snipeModel = snipe.ModelManager.FindAll(new SearchFilter { Search = modelName }).Rows.FirstOrDefault();
            }
            return snipeModel;
        }

        /// <summary>
        /// Synchronizes the specified model details with an existing 
        /// <c>Snipt.It</c> category or creates a new one if no category exists.
        /// </summary>
        /// <param name="snipe">
        ///     The <see cref="SnipeItApi"/> instance used to access the <c>Snipt.It</c> 
        ///     web API.
        /// </param>
        /// <param name="categoryName">
        ///     The category name.
        /// </param>
        /// <param name="categoryType">
        ///     The category type.
        /// </param>
        /// <returns>
        ///     The resulting <c>Snipt.It</c> <see cref="Category"/>.
        /// </returns>
        public static Category SyncCategory(
            this SnipeItApi snipe, 
            string categoryName, 
            string categoryType)
        {
            // Validate parameters.
            if (snipe == null)
            {
                throw new ArgumentNullException("snipe");
            }
            if (String.IsNullOrWhiteSpace(categoryName))
            {
                throw new ArgumentNullException("assetCategory");
            }

            Category snipeCategory = snipe.CategoryManager.FindAll(new SearchFilter { Search = categoryName })?.Rows.FirstOrDefault();
            if (snipeCategory == null)
            {
                snipeCategory = new Category() { Name = categoryName, Type = categoryType };
                var response = snipe.CategoryManager.Create(snipeCategory);
                snipeCategory = snipe.CategoryManager.FindAll(new SearchFilter { Search = categoryName })?.Rows.FirstOrDefault();
            }
            return snipeCategory;
        }

        /// <summary>
        /// Checks out / Associates the <c>Snipt.It</c> component matching the specified component serial
        /// with a <c>Snipt.It</c> asset matching the specified asset serial.
        /// </summary>
        /// <param name="snipe">
        ///     The <see cref="SnipeItApi"/> instance used to access the <c>Snipt.It</c> 
        ///     web API.
        /// </param>
        /// <param name="componentSerial">
        ///     The serial of the <c>Snipt.It</c> component to associate.
        /// </param>
        /// <param name="assetSerial">
        ///     The serial of the <c>Snipt.It</c> asset to associate.
        /// </param>
        public static void CheckOutComponent(
            this SnipeItApi snipe,
            string componentSerial,
            string assetSerial)
        {
            // Validate parameters.
            if (snipe == null)
            {
                throw new ArgumentNullException("snipe");
            }
            if (String.IsNullOrWhiteSpace(assetSerial))
            {
                throw new ArgumentNullException("assetSerial");
            }
            if (String.IsNullOrWhiteSpace(componentSerial))
            {
                throw new ArgumentNullException("componentSerial");
            }
            
            Asset snipeAsset = snipe.AssetManager.FindOne(new SearchFilter { Search = assetSerial });
            Component snipeComponent = snipe.ComponentManager.FindAll(new SearchFilter { Search = componentSerial, Limit = 1 })?.Rows.FirstOrDefault();
            if(snipeAsset == null)
            {
                throw new ArgumentOutOfRangeException("The specified asset does not exist in Snipe");
            }
            if (snipeComponent == null)
            {
                throw new ArgumentOutOfRangeException("The specified component does not exist in Snipe");
            }
            
            // The Snipe API does not have the capability to check-out components.
        }

        /// <summary>
        /// Maps the <see cref="DescriptorCategories"/> to a <c>Snipt.It</c> category.
        /// </summary>
        public static Dictionary<DescriptorCategories, string> DefaultSnipeCategoriesMap =
            new Dictionary<DescriptorCategories, string>()
            {
                {  DescriptorCategories.Desktops, "Desktops" },
                {  DescriptorCategories.Laptops, "Laptops" },
                {  DescriptorCategories.Servers, "Servers" },
                {  DescriptorCategories.Tablets, "Tablets" },
                {  DescriptorCategories.HardDrive, "HardDrive" },
                {  DescriptorCategories.Memory, "Memory" },
                {  DescriptorCategories.Processor, "Processor (CPU)" }
            };

        #endregion
    }
}
