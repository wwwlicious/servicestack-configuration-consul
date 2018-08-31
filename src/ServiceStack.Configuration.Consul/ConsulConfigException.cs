namespace ServiceStack.Configuration.Consul
{
    using System;

    /// <inheritdoc />
    /// <summary>
    /// Consul config related exception
    /// </summary>
    [Serializable]
    public class ConsulConfigException : Exception
    {
        /// <inheritdoc />
        public ConsulConfigException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public ConsulConfigException(string message, Exception e) : base(message, e)
        {
        }
    }
}