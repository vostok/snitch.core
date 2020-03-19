using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Commons.Time;

namespace Vostok.Snitch.Core.Tests
{
    [TestFixture]
    internal class LatencyClassifierTests
    {
        [Test]
        public void Should_classify_without_settings()
        {
            var classifier = new LatencyClassifier();
            classifier.IsSlow("service", 1.Seconds()).Should().BeFalse();
            classifier.IsSlow("service", 1.Minutes()).Should().BeTrue();
        }

        [TestCase("service", 99, false)]
        [TestCase("service", 100, true)]
        [TestCase("service", 101, true)]
        [TestCase("not_a_service", 9, false)]
        [TestCase("not_a_service", 10, true)]
        [TestCase("not_a_service", 11, true)]
        public void Should_classify_using_codes_from_settings(string service, int secondsLatency, bool expected)
        {
            var serviceSettings = new LatencyClassifierServiceSettings
            {
                SlowClassificationThreshold = 100.Seconds()
            };

            var settings = new LatencyClassifierSettings
            {
                SlowClassificationThreshold = 10.Seconds(),
                PerServiceSettings = new Dictionary<string, LatencyClassifierServiceSettings>
                {
                    ["service"] = serviceSettings
                }
            };

            new LatencyClassifier(() => settings).IsSlow(service, secondsLatency.Seconds()).Should().Be(expected);
        }
    }
}