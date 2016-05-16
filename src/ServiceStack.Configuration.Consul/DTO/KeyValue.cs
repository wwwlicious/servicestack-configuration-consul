// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul.DTO
{
    using System.Runtime.Serialization;
    using System.Text;
    using Text;

    [Route("/v1/kv/{Key}", "GET,PUT")]
    [DataContract]
    public class KeyValue : IUrlFilter
    {
        [DataMember]
        public string Key { get; set; }

        [DataMember(Name = "Value")]
        public byte[] RawValue { get; set; }

        [IgnoreDataMember]
        public string Value => Encoding.UTF8.GetString(RawValue ?? new byte[0]);

        public T GetValue<T>()
        {
            return TypeSerializer.DeserializeFromString<T>(Value);
        }

        public static KeyValue Create(string key)
        {
            return new KeyValue { Key = key };
        }

        public static KeyValue Create<T>(string key, T value)
        {
            return new KeyValue
            {
                Key = key,
                RawValue = Encoding.UTF8.GetBytes(TypeSerializer.SerializeToString(value))
            };
        }

        public string ToUrl(string absoluteUrl)
        {
            const string decodedSlash = "%2F";
            return Key.Contains("/") ? absoluteUrl.Replace(decodedSlash, "/") : absoluteUrl;
        } 
    }
}
