﻿namespace Firefly.CloudFormation.Tests.Unit.CloudFormation
{
    using System;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;
    using Amazon.S3;
    using Amazon.S3.Model;

    using Firefly.CloudFormation.CloudFormation;
    using Firefly.CloudFormation.Tests.Unit.resources;
    using Firefly.CloudFormation.Tests.Unit.Utils;
    using Firefly.CloudFormation.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;

    using TempFile = Firefly.CloudFormation.Tests.Unit.Utils.TempFile;

    public class Resolver
    {
        const string StackName = "test-stack";

        [Fact]
        public async Task ShouldResolveExistingTemplateWhenUsePreivousTemplateIsSelected()
        {
            var mockCf = new Mock<IAmazonCloudFormation>();

            mockCf.Setup(cf => cf.GetTemplateAsync(It.IsAny<GetTemplateRequest>(), default)).ReturnsAsync(
                new GetTemplateResponse { TemplateBody = EmbeddedResourceManager.GetResourceString("test-stack.json") });

            var mockClientFactory = new Mock<IAwsClientFactory>();

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCf.Object);

            var resolver = new TemplateResolver(mockClientFactory.Object, StackName, true);
            await resolver.ResolveFileAsync(null);

            resolver.Source.Should().Be(InputFileSource.UsePreviousTemplate);
            resolver.FileContent.Should().NotBeNullOrEmpty();
            resolver.ArtifactContent.Should().BeNull();
        }

        [Fact]
        public async Task ShouldResolveLocalFileTemplate()
        {
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var resolver = new TemplateResolver(mockClientFactory.Object, StackName, false);

            using var tempfile = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));
            await resolver.ResolveFileAsync(tempfile.Path);

            resolver.Source.Should().Be(InputFileSource.File);
            resolver.ArtifactContent.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ShouldResolveOversizeFileTemplateAsOversizeFile()
        {
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var resolver = new TemplateResolver(mockClientFactory.Object, StackName, false);

            using var tempfile = new TempFile(EmbeddedResourceManager.GetResourceStream("test-oversize.json"));
            await resolver.ResolveFileAsync(tempfile.Path);

            resolver.Source.Should().Be(InputFileSource.File | InputFileSource.Oversize);
            resolver.ArtifactContent.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ShouldResolveStringTemplate()
        {
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var resolver = new TemplateResolver(mockClientFactory.Object, StackName, false);

            var str = EmbeddedResourceManager.GetResourceString("test-stack.json");
            await resolver.ResolveFileAsync(str);

            resolver.Source.Should().Be(InputFileSource.String);
            resolver.ArtifactContent.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ShouldResolveOversizeStringTemplateAsOversizeString()
        {
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var resolver = new TemplateResolver(mockClientFactory.Object, StackName, false);

            var str = EmbeddedResourceManager.GetResourceString("test-oversize.json");
            await resolver.ResolveFileAsync(str);

            resolver.Source.Should().Be(InputFileSource.String | InputFileSource.Oversize);
            resolver.ArtifactContent.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [InlineData("https://s3.amazonaws.com/jbarr-public/template.json")]
        [InlineData("https://s3.amazonaws.com/jsb-public/template.json")]
        [InlineData("https://s3-us-east-2.amazonaws.com/jbarr-public/template.json")]
        [InlineData("https://jbarr-public.s3.amazonaws.com/template.json")]
        public async Task ShouldResolveHttpsUrlLocationAsS3(string url)
        {
            var mockS3 = new Mock<IAmazonS3>();
            mockS3.Setup(s3 => s3.GetObjectAsync(It.IsAny<GetObjectRequest>(), default)).ReturnsAsync(
                new GetObjectResponse { ResponseStream = EmbeddedResourceManager.GetResourceStream("test-stack.json") });

            var mockClientFactory = new Mock<IAwsClientFactory>();
            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(mockS3.Object);

            var resolver = new TemplateResolver(mockClientFactory.Object, StackName, false);

            await resolver.ResolveFileAsync(url);

            resolver.Source.Should().Be(InputFileSource.S3);
            resolver.ArtifactUrl.Should().Be(url);
            resolver.FileContent.Should().NotBeNullOrEmpty();
            resolver.ArtifactContent.Should().BeNull();
        }

        [Theory]
        [InlineData("s3://jbarr-public/template.json", "https://jbarr-public.s3.amazonaws.com/template.json")]
        public async Task ShouldResolveS3UrlLocationAsS3(string url, string resolvedUrl)
        {
            var mockS3 = new Mock<IAmazonS3>();
            mockS3.Setup(s3 => s3.GetObjectAsync(It.IsAny<GetObjectRequest>(), default)).ReturnsAsync(
                new GetObjectResponse { ResponseStream = EmbeddedResourceManager.GetResourceStream("test-stack.json") });

            var mockClientFactory = new Mock<IAwsClientFactory>();
            mockClientFactory.Setup(f => f.CreateS3Client()).Returns(mockS3.Object);

            var resolver = new TemplateResolver(mockClientFactory.Object, StackName, false);

            await resolver.ResolveFileAsync(url);

            resolver.Source.Should().Be(InputFileSource.S3);
            resolver.ArtifactUrl.Should().Be(resolvedUrl);
            resolver.FileContent.Should().NotBeNullOrEmpty();
            resolver.ArtifactContent.Should().BeNull();
        }

        [Theory]
        [InlineData("https://s3.amazonaws.com/template.json")]
        [InlineData("https://s3-us-east-2.amazonaws.com/template.json")]
        public void ShouldThrowArgumentExceptionForInvalidPathStyleUrl(string url)
        {
            var mockClientFactory = new Mock<IAwsClientFactory>();

            var resolver = new TemplateResolver(mockClientFactory.Object, StackName, false);

            Func<Task> action = async () => await resolver.ResolveFileAsync(url);

            action.Should().Throw<ArgumentException>().WithMessage(
                "'Path' style S3 URLs must have at least 2 path segments (bucketname/key)");
        }

        [Fact]
        public void ShouldThrowArgumentExceptionForInvalidVirtualHostStyleUrl()
        {
            var mockClientFactory = new Mock<IAwsClientFactory>();

            var resolver = new TemplateResolver(mockClientFactory.Object, StackName, false);

            Func<Task> action = async () => await resolver.ResolveFileAsync("https://jbarr-public.s3.amazonaws.com/");

            action.Should().Throw<ArgumentException>()
                .WithMessage("'Virtual Host' style S3 URLs must have at least 1 path segment (key)");
        }
    }
}