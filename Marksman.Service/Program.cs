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
            HostFactory.Run(configure =>
            {
                configure.Service<SnipeSniffService>(service =>
                {
                    service.ConstructUsing(ssService => new SnipeSniffService());
                    service.WhenStarted(ssService => ssService.StartAsync());
                    service.WhenStopped(ssService => ssService.Stop());
                });

                configure.RunAsLocalSystem();

                configure.SetServiceName("SnipeSniff Service");
                configure.SetDisplayName("SnipeSniff Service");
                configure.SetDescription("SnipeSniff Service");
            });          
        }
    }
}