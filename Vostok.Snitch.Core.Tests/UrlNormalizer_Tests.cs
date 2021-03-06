﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Snitch.Core.Tests
{
    [TestFixture]
    internal class UrlNormalizer_Tests
    {
        [TestCase("http://google.com/foo/5435453/bar", "foo/~/bar")]
        public void Should_normalize_path(string url, string expected)
        {
            new UrlNormalizer()
                .NormalizePath(null, new Uri(url, UriKind.Absolute))
                .Should()
                .Be(expected);
        }

        [TestCase(null, "http://google.com/foo/5435453/bar", "foo/~/bar")]
        [TestCase("cut", "http://google.com/not_a_foo/5435453/bar", "not_a_foo/~/bar")]
        [TestCase("cut", "http://google.com/foo/5435453/bar", "foo~")]
        [TestCase("cut", "http://google.com/foo1/5435453/bar", "foo~")]
        [TestCase("cut", "http://google.com/foo/5435453/bar", "foo~")]
        [TestCase("cut", "http://google.com/foo/5435453/bar/trash", "foo/~/bar/~")]
        [TestCase("cut", "http://google.com/slash/5435453/bar", "slash/~")]
        [TestCase("cut", "http://google.com/foo", "foo")]
        [TestCase("cut_all", "http://google.com/foo/5435453/bar", "~")]
        public void Should_cut_suffixes_for_service(string service, string url, string expected)
        {
            var settings = new UrlNormalizerSettings
            {
                PerServiceSettings = new Dictionary<string, UrlNormalizerServiceSettings>
                {
                    ["cut"] = new UrlNormalizerServiceSettings
                    {
                        FilteredPrefixes = new[] {"foo/~/bar/", "foo", "slash/"}
                    },
                    ["cut_all"] = new UrlNormalizerServiceSettings
                    {
                        FilteredPrefixes = new[] {""}
                    }
                }
            };

            new UrlNormalizer(() => settings)
                .NormalizePath(service, new Uri(url, UriKind.Absolute))
                .Should()
                .Be(expected);
        }

        [TestCase("http://google.com/track/~/~/open", "track/~/~/open")]
        [TestCase("http://google.com/track/guid/guid/open", "track/~")]
        public void Should_not_touch_already_normalized_urls(string url, string expected)
        {
            var settings = new UrlNormalizerSettings
            {
                PerServiceSettings = new Dictionary<string, UrlNormalizerServiceSettings>
                {
                    ["cut"] = new UrlNormalizerServiceSettings
                    {
                        FilteredPrefixes = new[] {"track/~/~/open", "track/"}
                    }
                }
            };

            new UrlNormalizer(() => settings)
                .NormalizePath("cut", new Uri(url, UriKind.Absolute))
                .Should()
                .Be(expected);
        }
    }
}