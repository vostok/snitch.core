using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Commons.Time;
using Vostok.Snitch.Core.Classifiers;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Core.Tests
{
    [TestFixture]
    internal class ResponseCodeClassifier_Tests
    {
        [Test]
        public void Should_classify_without_settings()
        {
            var classifier = new ResponseCodeClassifier();

            classifier.Classify("service", ResponseCode.Ok, 1.Seconds()).Should().Be(ResponseCodeClass.Success);
            classifier.Classify("service", ResponseCode.Conflict, 1.Seconds()).Should().Be(ResponseCodeClass.Warning);
            classifier.Classify("service", ResponseCode.InternalServerError, 1.Seconds()).Should().Be(ResponseCodeClass.Error);
        }

        [TestCase("service", ResponseCode.InternalServerError, ResponseCodeClass.Success)]
        [TestCase("service", ResponseCode.Created, ResponseCodeClass.Warning)]
        [TestCase("service", ResponseCode.Ok, ResponseCodeClass.Error)]
        [TestCase("service", ResponseCode.NotImplemented, ResponseCodeClass.Success)]
        [TestCase("service", ResponseCode.Accepted, ResponseCodeClass.Warning)]
        [TestCase("service", ResponseCode.NoContent, ResponseCodeClass.Error)]
        [TestCase("service", ResponseCode.PartialContent, ResponseCodeClass.Success)]
        [TestCase("not_a_service", ResponseCode.InternalServerError, ResponseCodeClass.Error)]
        [TestCase("not_a_service", ResponseCode.Created, ResponseCodeClass.Success)]
        [TestCase("not_a_service", ResponseCode.Ok, ResponseCodeClass.Success)]
        [TestCase("not_a_service", ResponseCode.NotImplemented, ResponseCodeClass.Success)]
        [TestCase("not_a_service", ResponseCode.Accepted, ResponseCodeClass.Warning)]
        [TestCase("not_a_service", ResponseCode.NoContent, ResponseCodeClass.Error)]
        [TestCase("not_a_service", ResponseCode.PartialContent, ResponseCodeClass.Success)]
        public void Should_classify_using_codes_from_settings(string service, ResponseCode code, ResponseCodeClass expected)
        {
            var serviceSettings = new ResponseCodeClassifierServiceSettings
            {
                SuccessCodes = new HashSet<ResponseCode> {ResponseCode.InternalServerError},
                WarningCodes = new HashSet<ResponseCode> {ResponseCode.Created},
                ErrorCodes = new HashSet<ResponseCode> {ResponseCode.Ok}
            };

            var settings = new ResponseCodeClassifierSettings
            {
                SuccessCodes = new HashSet<ResponseCode> {ResponseCode.NotImplemented},
                WarningCodes = new HashSet<ResponseCode> {ResponseCode.Accepted},
                ErrorCodes = new HashSet<ResponseCode> {ResponseCode.NoContent},
                PerServiceSettings = new Dictionary<string, ResponseCodeClassifierServiceSettings>
                {
                    ["service"] = serviceSettings
                }
            };

            var classifier = new ResponseCodeClassifier(() => settings);

            classifier.Classify(service, code, 1.Seconds()).Should().Be(expected);
        }

        [TestCase("service", 4, ResponseCodeClass.Warning)]
        [TestCase("service", 5, ResponseCodeClass.Error)]
        [TestCase("service", 6, ResponseCodeClass.Error)]
        [TestCase("not_a_service", 9, ResponseCodeClass.Warning)]
        [TestCase("not_a_service", 10, ResponseCodeClass.Error)]
        [TestCase("not_a_service", 11, ResponseCodeClass.Error)]
        public void Should_classify_using_timeout_error_classification_threshold_setting(string service, int secondsLatency, ResponseCodeClass expected)
        {
            var serviceSettings = new ResponseCodeClassifierServiceSettings
            {
                TimeoutErrorClassificationThreshold = 5.Seconds()
            };

            var settings = new ResponseCodeClassifierSettings
            {
                TimeoutErrorClassificationThreshold = 10.Seconds(),
                PerServiceSettings = new Dictionary<string, ResponseCodeClassifierServiceSettings>
                {
                    ["service"] = serviceSettings
                }
            };

            var classifier = new ResponseCodeClassifier(() => settings);

            classifier.Classify(service, ResponseCode.RequestTimeout, secondsLatency.Seconds()).Should().Be(expected);
        }
    }
}