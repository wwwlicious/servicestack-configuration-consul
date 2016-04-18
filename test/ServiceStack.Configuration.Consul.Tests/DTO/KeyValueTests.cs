
namespace ServiceStack.Configuration.Consul.Tests.DTO
{
    using System.Text;
    using Consul.DTO;
    using FluentAssertions;
    using Text;
    using Xunit;

    public class KeyValueTests
    {
        private const string TestKey = "keykey124";

        [Fact]
        public void KeyValue_HasRouteAttribute()
        {
            var routeAttribute = typeof(KeyValue).FirstAttribute<RouteAttribute>();
            routeAttribute.Should().NotBeNull();
        }

        [Fact]
        public void KeyValue_HasRouteAttribute_WithCorrectRoute()
        {
            var routeAttribute = typeof(KeyValue).FirstAttribute<RouteAttribute>();
            routeAttribute.Path.Should().Be("/v1/kv/{Key}");
        }

        [Fact]
        public void KeyValue_HasRouteAttribute_WithGetAndPutVerbs()
        {
            var routeAttribute = typeof(KeyValue).FirstAttribute<RouteAttribute>();
            routeAttribute.Verbs.Should().Be("GET,PUT");
        }

        [Fact]
        public void Create_Key_SetsKey()
        {
            var key = KeyValue.Create(TestKey);

            key.Key.Should().Be(TestKey);
        }

        [Fact]
        public void Create_Key_DoesNotSetRawValue()
        {
            var key = KeyValue.Create(TestKey);

            key.RawValue.Should().BeNull();
        }

        [Fact]
        public void Create_KeyValue_SetsKey()
        {
            var key = KeyValue.Create(TestKey, TestKey);

            key.Key.Should().Be(TestKey);
        }

        [Fact]
        public void Create_KeyValue_SetsRawValue()
        {
            const int value = 123123;
            var key = KeyValue.Create(TestKey, value);

            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.SerializeToString(value));
            key.RawValue.Should().BeEquivalentTo(bytes);
        }

        [Fact]
        public void Value_ReturnsBytesAsString_ValueType()
        {
            const int value = 123123;
            var key = KeyValue.Create(TestKey, value);

            key.Value.Should().Be(value.ToString());
        }

        [Fact]
        public void Value_ReturnsBytesAsString_ComplexType()
        {
            var human = new Human { Name = "Toddler", Age = 2 };
            var key = KeyValue.Create(TestKey, human);

            key.Value.Should().Be("{Age:2,Name:Toddler}");
        }

        [Fact]
        public void GetValue_Object_ComplexType()
        {
            var human = new Human { Name = "Toddler", Age = 2 };
            var key = KeyValue.Create(TestKey, human);

            var result = key.GetValue<Human>();
            result.Name.Should().Be("Toddler");
            result.Age.Should().Be(2);
        }
    }
}
