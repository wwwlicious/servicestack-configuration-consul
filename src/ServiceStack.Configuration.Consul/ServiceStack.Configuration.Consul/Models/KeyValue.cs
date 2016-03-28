namespace ServiceStack.Configuration.Consul.Models
{
    using System.Runtime.Serialization;
    using System.Text;
    using Text;

    [Route("/v1/kv/{Key}", "GET,PUT")]
    public class KeyValue
    {
        public string Key { get; set; }

        public byte[] Value { get; set; }

        [IgnoreDataMember]
        // NOTE Do I need this?
        public string ValueString => Encoding.UTF8.GetString(Value);

        public T GetValueAs<T>()
        {
            return JsonSerializer.DeserializeFromString<T>(ValueString);
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
                Value = Encoding.UTF8.GetBytes(JsonSerializer.SerializeToString(value))
            };
        }
    }
}

