namespace ServiceStack.Configuration.Consul
{
    public enum LookupStrategy
    {
        /// <summary>
        /// The provided key will be used as direct lookup
        /// </summary>
        BasicLookup = 0,

        /// <summary>
        /// The provided key will be used along with service name/instance id to try and find most specific value
        /// </summary>
        Fallthrough = 2
    }
}