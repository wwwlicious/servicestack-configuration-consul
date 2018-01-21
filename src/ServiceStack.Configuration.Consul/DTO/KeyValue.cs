// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.DTO
{
    using System.Runtime.Serialization;
    using System.Text;
    using Text;

    /// <summary>
    /// DTO for transferring key/values with consul
    /// </summary>
    [Route("/v1/kv/{Key}", "GET,PUT")]
    [DataContract]
    public class KeyValue : IUrlFilter
    {
        /// <summary>
        /// The key name
        /// </summary>
        [DataMember]
        public string Key { get; set; }

        /// <summary>
        /// The key value
        /// </summary>
        [DataMember(Name = "Value")]
        public byte[] RawValue { get; set; }

        /// <summary>
        /// returns the value as a string
        /// </summary>
        [IgnoreDataMember]
        public string Value => Encoding.UTF8.GetString(RawValue ?? new byte[0]);

        /// <summary>
        /// Gets the value
        /// </summary>
        /// <typeparam name="T">the value type</typeparam>
        /// <returns>the key value</returns>
        public T GetValue<T>()
        {
            return TypeSerializer.DeserializeFromString<T>(Value);
        }

        /// <summary>
        /// Creates a key
        /// </summary>
        /// <param name="key">the key name</param>
        /// <returns>the keyvalue dto</returns>
        public static KeyValue Create(string key)
        {
            return new KeyValue { Key = key };
        }

        /// <summary>
        /// Creates a key
        /// </summary>
        /// <param name="key">the key name</param>
        /// <param name="value">the key value</param>
        /// <typeparam name="T">the value type</typeparam>
        /// <returns>the keyvalue dto</returns>
        public static KeyValue Create<T>(string key, T value)
        {
            return new KeyValue
            {
                Key = key,
                RawValue = Encoding.UTF8.GetBytes(TypeSerializer.SerializeToString(value))
            };
        }

        /// <inheritdoc />
        public string ToUrl(string absoluteUrl)
        {
            const string decodedSlash = "%2F";
            return Key.Contains("/") ? absoluteUrl.Replace(decodedSlash, "/") : absoluteUrl;
        } 
    }
}
