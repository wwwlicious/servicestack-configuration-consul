// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using DTO;

    internal static class KeyValueExtensions
    {
        internal static Dictionary<string, string> ConvertToDictionary(this IEnumerable<KeyValue> keyValues)
            => keyValues.ToDictionary(k => k.Key, v => v.GetValue<object>().ToString());
    }
}
