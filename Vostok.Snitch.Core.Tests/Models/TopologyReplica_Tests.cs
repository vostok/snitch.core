using FluentAssertions;
using NUnit.Framework;
using Vostok.Commons.Helpers.Url;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Core.Tests.Models
{
    [TestFixture]
    internal class TopologyReplica_Tests
    {
        [TestCase("http://google.com/", "http://google.com/", true)]
        [TestCase("http://google.com/a", "http://google.com/a", true)]
        [TestCase("http://google.com/a", "http://GOOGLE.com/A", true)]
        [TestCase("http://google.com/", "http://google.com/asdf", true)]
        [TestCase("http://google.com/a?qwe", "http://google.com/a?xyz", true)]

        [TestCase("http://google1.com/", "http://google2.com/", false)]
        [TestCase("http://google.com:100/", "http://google.com:101/", false)]
        [TestCase("http://google.com/a", "http://google.com/b", false)]
        [TestCase("http://google.com/a?qwe", "http://google.com/b?xyz", false)]
        public void Should_be_comparable(string url1, string url2, bool expected)
        {
            var r1 = new TopologyReplica(UrlParser.Parse(url1));
            var r2 = new TopologyReplica(UrlParser.Parse(url2));
            r1.Equals(r2).Should().Be(expected);

            if (expected)
                r1.GetHashCode().Should().Be(r2.GetHashCode());
        }
    }
}