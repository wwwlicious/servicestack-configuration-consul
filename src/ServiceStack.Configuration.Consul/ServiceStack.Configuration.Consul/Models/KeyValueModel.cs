namespace ServiceStack.Configuration.Consul.Models
{
    internal class KeyValueModel
    {
        public string Key { get; set; }
        public byte[] Value { get; set; }
    }
}
