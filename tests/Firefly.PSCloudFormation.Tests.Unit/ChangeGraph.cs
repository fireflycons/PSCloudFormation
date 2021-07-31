﻿namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Firefly.EmbeddedResourceLoader;
    using Firefly.PSCloudFormation.ChangeVisualisation;

    using FluentAssertions;

    using Newtonsoft.Json;

    using Xunit;
    using Xunit.Abstractions;

    public class ChangeGraph : AutoResourceLoader
    {
        private readonly ITestOutputHelper output;

#pragma warning disable 649
        [EmbeddedResource("changeset.json")]
        private string changeJson;

        public ChangeGraph(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async void ShouldConnectToQuickChartIO()
        {
            var renderer = new QuickChartSvgRenderer();

            var stat = await renderer.GetStatus();

            stat.Should().Be(RendererStatus.Ok);
        }

        [SkippableFact]
        public async void ShouldRenderLargeGraph()
        {
            var changeDetails = JsonConvert.DeserializeObject<List<ChangesetDetails>>(this.changeJson).First();
            var renderer = new QuickChartSvgRenderer();
            var ok = await renderer.GetStatus() == RendererStatus.Ok;

            Skip.IfNot(ok, "Could not render SVG. Check rendering API https://quickchart.io/graphviz");

            var svg = await changeDetails.RenderSvg(renderer);

            svg.Should().NotBeNull();
        }

        [Fact]
        public async void ShouldRenderChartMarkup()
        {
            var renderer = new QuickChartSvgRenderer();

            var svg = await renderer.RenderSvg("digraph G {0[label = <<S>P: SubnetCidr</S>>]}");

            svg.ToString().Should().Contain("text-decoration=\"line-through\"");
        }
        
        [Fact]
        public async void ShouldRenderSimpleGraph()
        {
            var renderer = new QuickChartSvgRenderer();

            var svg = await renderer.RenderSvg("graph{a--b}");

            svg.Should().NotBeNull();
        }

        [Fact]
        public void ShouldFormatHtlmDot()
        {
            const string Dot = "digraph G {0[label = \"<S>P: SubnetCidr</S>\"]}";
            const string Expected = "digraph G {0[label = <<S>P: SubnetCidr</S>>]}";

            var formattedDot = DotHtmlFormatter.QuoteHtml(Dot);

            formattedDot.Should().Be(Expected);
        }
    }
}