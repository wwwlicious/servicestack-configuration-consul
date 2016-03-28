
namespace ServiceStack.Configuration.Consul.IntTests
{
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Consul")]
    public class ConsulAppSettingsTests
    {
        private readonly IAppSettings appSettings;
        public ConsulAppSettingsTests()
        {
            appSettings = new ConsulAppSettings("http://127.0.0.1:8500/");
        }

        // TODO Setup a series of calls to configure the test suite? As a Fixture.
        // Use a prefix to enable easy cleaning??

        [Fact]
        public void Get_String_ReturnsCorrectString()
        {
            const string expected = "testString";
            const string key = "string1";

            var actual = appSettings.GetString(key);

            actual.Should().Be(expected);
        }

        [Fact]
        public void Get_String_GetsJsonString_IfComplexModel()
        {
            const string expected = "{ \"Age\": 34, \"Name\": \"Donald\" }";
            const string key = "person1";

            var actual = appSettings.GetString(key);

            actual.Should().Be(expected);
        }

        [Fact]
        public void Get_String_ReturnsNullIfNotFound()
        {
            const string key = "unknownKey";

            var actual = appSettings.GetString(key);

            actual.Should().BeNull();
        }

        [Fact]
        public void Get_T_ReturnsCorrectObject()
        {
            const string key = "person1";

            var actual = appSettings.Get<Dog>(key);
        }

        [Fact]
        public void Get_T_ReturnsNullIfNotFound_ReferenceType()
        {
            const string key = "unknownperson1";

            var actual = appSettings.Get<Human>(key);

            actual.Should().BeNull();
        }

        [Fact]
        public void Set()
        {
            const string key = "person44";
            const string value = "value99";

            appSettings.Set(key, value);

            appSettings.GetString(key).Should().Be(value);
        }

        [Fact]
        public void Set2()
        {
            const string key = "person44123";

            var x = new Human { Age = 99, Name = "Test Person" };

            appSettings.Set(key, x);

            var human = appSettings.Get<Human>(key);
            human.Should().Be(x);
        }
    }

    public class Dog
    {
        public int DogYears { get; set; }
        public int Name { get; set; }
    }

    public class Human
    {
        public int Age { get; set; }
        public string Name { get; set; }
    }
}