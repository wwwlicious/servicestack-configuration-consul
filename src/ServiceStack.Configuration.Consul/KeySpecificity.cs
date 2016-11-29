// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul
{
    /// <summary>
    /// Represents specificity level for Set commands
    /// </summary>
    public enum KeySpecificity
    {
        /// <summary>
        /// No amendments are made to key, Set command called with key as-is
        /// </summary>
        LiteralKey = 0,

        /// <summary>
        /// Values only updated for this specific instance of the service
        /// </summary>
        Instance = 1,

        /// <summary>
        /// Values updated for all instances of this service with same version number (set in HostConfig.ApiVersion)
        /// </summary>
        Version = 2,

        /// <summary>
        /// All instances of this service will be able to access key
        /// </summary>
        Service = 4,

        /// <summary>
        /// Any instance of any service can access key
        /// </summary>
        Global = 8
    }
}