namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.Collections.Generic;

    using Firefly.PSCloudFormation.LambdaPackaging.PEP508;
    using Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model;

    using sly.parser;
    using sly.parser.generator;

    using Xunit;
    using Xunit.Abstractions;

    public class PEP508ParserTests
    {
        private readonly Parser<MetadataToken, IExpression> parser;

        private readonly ITestOutputHelper testOutputHelper;

        private readonly ExpressionContext variableContext = new ExpressionContext(
            new Dictionary<string, string>
                {
                    { "python_version", "2.7" },
                    { "implementation_name", "cpython" },
                    { "platform_python_implementation", "CPython" },
                    { "sys_platform", "win32" },
                    { "platform_system", "Darwin" },
                    { "extra", "librabbitmq" }
                });

        public PEP508ParserTests(ITestOutputHelper helper)
        {
            this.testOutputHelper = helper;
            var parserInstance = new PEP508Parser();
            var builder = new ParserBuilder<MetadataToken, IExpression>();
            var build = builder.BuildParser(parserInstance, ParserType.LL_RECURSIVE_DESCENT, "logical_expression");

            if (build.IsError)
            {
                foreach (var error in build.Errors)
                {
                    this.testOutputHelper.WriteLine(error.Message);
                }
            }

            Assert.False(build.IsError);
            this.parser = build.Result;
        }

        [Theory]
        [InlineData("(platform_python_implementation == \"CPython\") and extra == 'brotli'", false)]
        [InlineData("(platform_python_implementation == \"PyPy\") and extra == 'brotli'", false)]
        [InlineData("(platform_system != \"Windows\") and extra == 'memcache'", false)]
        [InlineData("(python_version < \"3.3\") and extra == 'lzma'", false)]
        [InlineData("(python_version < \"3.5\") and extra == 'tests'", false)]
        [InlineData("(python_version < \"3.8.0\") and extra == 'tblib'", false)]
        [InlineData("(python_version < \"3.9\") and extra == 'testing'", false)]
        [InlineData("(python_version == \"2.7\") and extra == 'secure'", false)]
        [InlineData("(python_version == \"2.7\") and extra == 'testing'", false)]
        [InlineData("(python_version == \"2.7\") and extra == 'tests'", false)]
        [InlineData("(python_version >= \"3.5\") and extra == 'tests'", false)]
        [InlineData("(python_version >= \"3.6\") and extra == 'tests'", false)]
        [InlineData("(python_version >= \"3.8.0\") and extra == 'tblib'", false)]
        [InlineData("(sys_platform == \"win32\") and extra == 'ssl'", false)]
        [InlineData("extra == \"colors\"", false)]
        [InlineData("extra == \"pipfile_deprecated_finder\" or extra == \"requirements_deprecated_finder\"", false)]
        [InlineData("extra == \"pipfile_deprecated_finder\"", false)]
        [InlineData("extra == \"requirements_deprecated_finder\"", false)]
        [InlineData("extra == 'all'", false)]
        [InlineData("extra == 'arangodb'", false)]
        [InlineData("extra == 'auth'", false)]
        [InlineData("extra == 'azureblockblob'", false)]
        [InlineData("extra == 'azureservicebus'", false)]
        [InlineData("extra == 'azurestoragequeues'", false)]
        [InlineData("extra == 'brotli'", false)]
        [InlineData("extra == 'build'", false)]
        [InlineData("extra == 'cassandra'", false)]
        [InlineData("extra == 'certs'", false)]
        [InlineData("extra == 'consul'", false)]
        [InlineData("extra == 'cosmosdbsql'", false)]
        [InlineData("extra == 'couchbase'", false)]
        [InlineData("extra == 'couchdb'", false)]
        [InlineData("extra == 'cryptography'", false)]
        [InlineData("extra == 'dev'", false)]
        [InlineData("extra == 'django'", false)]
        [InlineData("extra == 'doc'", false)]
        [InlineData("extra == 'docs'", false)]
        [InlineData("extra == 'docstest'", false)]
        [InlineData("extra == 'dynamodb'", false)]
        [InlineData("extra == 'elasticsearch'", false)]
        [InlineData("extra == 'eventlet'", false)]
        [InlineData("extra == 'format'", false)]
        [InlineData("extra == 'format_nongpl'", false)]
        [InlineData("extra == 'gdal'", false)]
        [InlineData("extra == 'gevent'", false)]
        [InlineData("extra == 'i18n'", false)]
        [InlineData("extra == 'kernel'", false)]
        [InlineData("extra == 'librabbitmq'", true)]
        [InlineData("extra == 'lxml'", false)]
        [InlineData("extra == 'matplotlib'", false)]
        [InlineData("extra == 'mongodb'", false)]
        [InlineData("extra == 'msgpack'", false)]
        [InlineData("extra == 'nbconvert'", false)]
        [InlineData("extra == 'nbformat'", false)]
        [InlineData("extra == 'notebook'", false)]
        [InlineData("extra == 'numpy'", false)]
        [InlineData("extra == 'pandas'", false)]
        [InlineData("extra == 'parallel'", false)]
        [InlineData("extra == 'pep8test'", false)]
        [InlineData("extra == 'pycrypto'", false)]
        [InlineData("extra == 'pycryptodome'", false)]
        [InlineData("extra == 'pydot'", false)]
        [InlineData("extra == 'pygraphviz'", false)]
        [InlineData("extra == 'pymemcache'", false)]
        [InlineData("extra == 'pyro'", false)]
        [InlineData("extra == 'pytest'", false)]
        [InlineData("extra == 'pyyaml'", false)]
        [InlineData("extra == 'qa'", false)]
        [InlineData("extra == 'qpid'", false)]
        [InlineData("extra == 'qtconsole'", false)]
        [InlineData("extra == 'redis'", false)]
        [InlineData("extra == 's3'", false)]
        [InlineData("extra == 'scipy'", false)]
        [InlineData("extra == 'secure'", false)]
        [InlineData("extra == 'security'", false)]
        [InlineData("extra == 'server'", false)]
        [InlineData("extra == 'slmq'", false)]
        [InlineData("extra == 'socks'", false)]
        [InlineData("extra == 'solar'", false)]
        [InlineData("extra == 'sqlalchemy'", false)]
        [InlineData("extra == 'sqs'", false)]
        [InlineData("extra == 'ssh'", false)]
        [InlineData("extra == 'test'", false)]
        [InlineData("extra == 'testing'", false)]
        [InlineData("extra == 'testing.libs'", false)]
        [InlineData("extra == 'tests'", false)]
        [InlineData("extra == 'tls'", false)]
        [InlineData("extra == 'watchdog'", false)]
        [InlineData("extra == 'yaml'", false)]
        [InlineData("extra == 'zookeeper'", false)]
        [InlineData("extra == 'zstd'", false)]
        [InlineData("implementation_name == \"cpython\" and python_version < \"3.8\"", true)]
        [InlineData("platform_system == \"Darwin\"", true)]
        [InlineData("python_version != \"3.4\"", true)]
        [InlineData("python_version < \"3.2\"", true)]
        [InlineData("python_version < \"3.3\"", true)]
        [InlineData("python_version < \"3.4\"", true)]
        [InlineData("python_version < \"3.5\"", true)]
        [InlineData("python_version < \"3.7\" and python_version != \"3.4\"", true)]
        [InlineData("python_version < \"3.8\"", true)]
        [InlineData("python_version < \"3\"", true)]
        [InlineData("python_version < '3'", true)]
        [InlineData("python_version <= \"3.4\"", true)]
        [InlineData("python_version == \"3.4\"", false)]
        [InlineData("python_version >= \"3.5\"", false)]
        [InlineData("python_version in \"2.6 2.7 3.2 3.3\"", true)]
        [InlineData("python_version!='3.4'", true)]
        [InlineData("python_version<'3.3'", true)]
        [InlineData("python_version==\"2.7\"", true)]
        [InlineData("python_version=='3.4'", false)]
        [InlineData("sys_platform != \"win32\"", false)]
        [InlineData("sys_platform == \"darwin\"", false)]
        [InlineData("sys_platform == \"win32\" and python_version == \"2.7\" and extra == 'socks'", false)]
        [InlineData("sys_platform == \"win32\"", true)]
        [InlineData("sys_platform==\"win32\"", true)]
        public void TestMulptipleExpressions(string expression, bool expectedResult)
        {
            var r = this.parser.Parse(expression);

            if (r.IsError)
            {
                foreach (var error in r.Errors)
                {
                    this.testOutputHelper.WriteLine(error.ErrorMessage);
                }
            }

            Assert.False(r.IsError);
            Assert.NotNull(r.Result);
            Assert.Equal(expectedResult, r.Result.Evaluate(this.variableContext));
        }
    }
}