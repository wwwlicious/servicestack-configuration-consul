namespace ServiceStack.Configuration.Consul.DTO
{
    using System.Runtime.Serialization;
    using System.Text;
    using Text;

    [Route("/v1/kv/{Key}", "GET,PUT")]
    [DataContract]
    public class KeyValue
    {
        [DataMember]
        public string Key { get; set; }

        [DataMember(Name = "Value")]
        public byte[] RawValue { get; set; }

        [IgnoreDataMember]
        public string Value => Encoding.UTF8.GetString(RawValue);

        public T GetValue<T>()
        {
            return JsonSerializer.DeserializeFromString<T>(Value);
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
                RawValue = Encoding.UTF8.GetBytes(JsonSerializer.SerializeToString(value))
            };
        }
    }
}
