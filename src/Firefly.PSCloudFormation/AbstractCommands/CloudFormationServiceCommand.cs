// ReSharper disable StyleCop.SA1126
// ReSharper disable StyleCop.SA1305
// ReSharper disable InconsistentNaming

namespace Firefly.PSCloudFormation.AbstractCommands
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Management.Automation;

    using Amazon;
    using Amazon.CloudFormation;
    using Amazon.PowerShell.Common;
    using Amazon.Runtime;
    using Amazon.Runtime.CredentialManagement;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Model;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// <para>
    /// Service level base class. Contains much of the implementation for determining credentials and region from AWS.Tools.Common
    /// This was re-implemented here rather than just inheriting from AWS <c>ServiceCmdlet"</c> as they don't do this evaluation until the pipeline is being processed.
    /// We need this information earlier in order to be able to process dynamic parameters when updating with Use Previous Template, i.e. we need to be able to retrieve
    /// existing template from CloudFormation.
    /// </para>
    /// <para>
    /// WARNING: May cause issues if the credential arguments are being read from an object in the pipeline, as in the object may not yet be resolved.
    /// </para>
    /// </summary>
    /// <seealso cref="System.Management.Automation.PSCmdlet" />
    public abstract class CloudFormationServiceCommand : PSCmdlet, IAWSCredentialsArguments, IAWSRegionArguments
    {
        /// <summary>
        /// The path resolver
        /// </summary>
        private IPathResolver pathResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFormationServiceCommand"/> class.
        /// </summary>
        protected CloudFormationServiceCommand()
        {
            this.Logger = new PSLogger(this);
        }

        /// <summary>
        /// Gets or sets the access key.
        /// <para type="description">
        /// The AWS access key for the user account. This can be a temporary access key
        /// if the corresponding session token is supplied to the -SessionToken parameter.
        /// </para>
        /// </summary>
        [SuppressParameterSelect]
        [Alias("AK")]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string AccessKey { get; set; }

        /// <summary>
        /// Gets or sets the credential.
        /// <para type="description">
        /// An AWSCredentials object instance containing access and secret key information,
        /// and optionally a token for session-based credentials.
        /// </para>
        /// </summary>
        [SuppressParameterSelect]
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public AWSCredentials Credential { get; set; }

        /// <summary>
        /// Gets or sets the endpoint URL.
        /// <para type="description">
        /// The endpoint to make CloudFormation calls against.
        /// </para>
        /// <para type="description">
        /// The cmdlets normally determine which endpoint to call based on the region specified to the -Region parameter or set as default in the shell (via Set-DefaultAWSRegion).
        /// Only specify this parameter if you must direct the call to a specific custom endpoint, e.g. if using LocalStack or some other AWS emulator or a VPC endpoint from an EC2 instance.
        /// </para>
        /// </summary>
        /// <value>
        /// The endpoint URL.
        /// </value>
        [SuppressParameterSelect]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the network credential.
        /// <para type="description">
        /// Used with SAML-based authentication when ProfileName references a SAML role profile. 
        /// Contains the network credentials to be supplied during authentication with the 
        /// configured identity provider's endpoint. This parameter is not required if the
        /// user's default network identity can or should be used during authentication.
        /// </para>
        /// </summary>
        [SuppressParameterSelect]
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public PSCredential NetworkCredential { get; set; }

        /// <summary>
        /// Gets or sets the profile location.
        /// <para type="description">
        /// Used to specify the name and location of the ini-format credential file (shared with
        /// the AWS CLI and other AWS SDKs)
        /// </para>
        /// <para type="description">
        /// If this optional parameter is omitted this cmdlet will search the encrypted credential
        /// file used by the AWS SDK for .NET and AWS Toolkit for Visual Studio first.
        /// If the profile is not found then the cmdlet will search in the ini-format credential
        /// file at the default location: (user's home directory)\.aws\credentials.
        /// </para>
        /// <para type="description">
        /// If this parameter is specified then this cmdlet will only search the ini-format credential
        /// file at the location given.
        /// </para>
        /// <para type="description">
        /// As the current folder can vary in a shell or during script execution it is advised
        /// that you use specify a fully qualified path instead of a relative path.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Note that the encrypted credential file is not supported on all platforms.
        /// It will be skipped when searching for profiles on Windows Nano Server, Mac, and Linux platforms.
        /// </remarks>
        [SuppressParameterSelect]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("AWSProfilesLocation", "ProfilesLocation")]
        public string ProfileLocation { get; set; }

        /// <summary>
        /// Gets or sets the profile name.
        /// <para type="description">
        /// The user-defined name of an AWS credentials or SAML-based role profile containing
        /// credential information. The profile is expected to be found in the secure credential
        /// file shared with the AWS SDK for .NET and AWS Toolkit for Visual Studio. You can also
        /// specify the name of a profile stored in the .ini-format credential file used with 
        /// the AWS CLI and other AWS SDKs.
        /// </para>
        /// </summary>
        [SuppressParameterSelect]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("StoredCredentials", "AWSProfileName")]
        public string ProfileName { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// <para type="description">
        /// The system name of an AWS region or an AWSRegion instance. This governs
        /// the endpoint that will be used when calling service operations. Note that 
        /// the AWS resources referenced in a call are usually region-specific.
        /// </para>
        /// </summary>
        [SuppressParameterSelect]
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Alias("RegionToCall")]
        public object Region { get; set; }

        /// <summary>
        /// Gets or sets the s3 endpoint URL.
        /// <para type="description">
        /// The endpoint to make S3 calls against. 
        /// </para>
        /// <para type="description">
        /// S3 is used by these cmdlets for managing S3 based templates and by the packager for uploading code artifacts and nested templates.
        /// </para>
        /// <para type="description">
        /// The cmdlets normally determine which endpoint to call based on the region specified to the -Region parameter or set as default in the shell (via Set-DefaultAWSRegion).
        /// Only specify this parameter if you must direct the call to a specific custom endpoint, e.g. if using LocalStack or some other AWS emulator or a VPC endpoint from an EC2 instance.
        /// </para>
        /// </summary>
        /// <value>
        /// The s3 endpoint URL.
        /// </value>
        [SuppressParameterSelect]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string S3EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the secret key
        /// <para type="description">
        /// The AWS secret key for the user account. This can be a temporary secret key
        /// if the corresponding session token is supplied to the -SessionToken parameter.
        /// </para>
        /// </summary>
        [SuppressParameterSelect]
        [Alias("SK", "SecretAccessKey")]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string SecretKey { get; set; }

        /// <summary>
        /// Gets or sets the session token
        /// <para type="description">
        /// The session token if the access and secret keys are temporary session-based credentials.
        /// </para>
        /// </summary>
        [SuppressParameterSelect]
        [Alias("ST")]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string SessionToken { get; set; }

        /// <summary>
        /// Gets or sets the STS endpoint URL.
        /// <para type="description">
        /// The endpoint to make STS calls against. 
        /// </para>
        /// <para type="description">
        /// STS is used only if creating a bucket to store oversize templates and packager artifacts to get the caller account ID to use as part of the generated bucket name.
        /// </para>
        /// <para type="description">
        /// The cmdlets normally determine which endpoint to call based on the region specified to the -Region parameter or set as default in the shell (via Set-DefaultAWSRegion).
        /// Only specify this parameter if you must direct the call to a specific custom endpoint, e.g. if using LocalStack or some other AWS emulator or a VPC endpoint from an EC2 instance.
        /// </para>
        /// </summary>
        /// <value>
        /// The STS endpoint URL.
        /// </value>
        [SuppressParameterSelect]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once StyleCop.SA1650
        public string STSEndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        internal ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the instance of PowerShell implementation of path resolver using provider intrinsic
        /// </summary>
        internal IPathResolver PathResolver
        {
            get => this.pathResolver ?? (this.pathResolver = new PSPathResolver(this.SessionState));

            set => this.pathResolver = value;
        }

        /// <summary>
        /// Gets or sets the client factory.
        /// </summary>
        /// <value>
        /// The client factory.
        /// </value>
        protected IPSAwsClientFactory _ClientFactory { get; set; }

        /// <summary>
        /// Gets or sets the current credentials.
        /// </summary>
        /// <value>
        /// The current credentials.
        /// </value>
        protected AWSCredentials _CurrentCredentials { get; set; }

        /// <summary>
        /// Gets or sets the current region.
        /// </summary>
        /// <value>
        /// The current region.
        /// </value>
        protected RegionEndpoint _CurrentRegion { get; set; }

        /// <summary>
        /// Gets the default region.
        /// </summary>
        /// <value>
        /// The default region.
        /// </value>
        protected virtual string _DefaultRegion => null;

        /// <summary>
        /// Gets or sets the region endpoint.
        /// </summary>
        /// <value>
        /// The region endpoint.
        /// </value>
        protected RegionEndpoint _RegionEndpoint { get; set; }

        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        protected IPSCloudFormationContext Context { get; private set; }

        /// <summary>
        /// Gets the credential profile options.
        /// </summary>
        /// <returns>Access key credential options as a struct</returns>
        public CredentialProfileOptions GetCredentialProfileOptions()
        {
            return new CredentialProfileOptions
            {
                AccessKey = this.AccessKey,
                SecretKey = this.SecretKey,
                Token = this.SessionToken
            };
        }

        /// <summary>
        /// Creates the CloudFormation client using AWS Tools library.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="region">The region.</param>
        /// <returns>CloudFormation client.</returns>
        protected IAmazonCloudFormation CreateClient(AWSCredentials credentials, RegionEndpoint region)
        {
            var amazonCloudFormationConfig = new AmazonCloudFormationConfig { RegionEndpoint = region };

            //Common.PopulateConfig(this, amazonCloudFormationConfig);
            this.CustomizeClientConfig(amazonCloudFormationConfig);
            var amazonCloudFormationClient = new AmazonCloudFormationClient(credentials, amazonCloudFormationConfig);
            return amazonCloudFormationClient;
        }

        /// <summary>
        /// Creates the cloud formation context.
        /// </summary>
        /// <returns>New <see cref="IPSCloudFormationContext"/></returns>
        protected IPSCloudFormationContext CreateCloudFormationContext()
        {
            if (this.Context != null)
            {
                return this.Context;
            }

            AWSCredentials credentials;

            if (this._CurrentCredentials != null)
            {
                credentials = this._CurrentCredentials;
            }
            else if (this.TryGetCredentials(this.Host, out var psCredentials, this.SessionState))
            {
                this.Logger.LogDebug($"Acquired credentials '{psCredentials.Name}' from {psCredentials.Source}");
                this._CurrentCredentials = psCredentials.Credentials;
                credentials = this._CurrentCredentials;
            }
            else
            {
                this.ThrowExecutionError(
                    "No credentials specified or obtained from persisted/shell defaults.",
                    this,
                    null);
                return null;
            }

            if (this._CurrentRegion == null)
            {
                this.TryGetRegion(true, out var region, out var regionSource, this.SessionState);
                this._RegionEndpoint = region;

                if (this._RegionEndpoint == null)
                {
                    if (string.IsNullOrEmpty(this._DefaultRegion))
                    {
                        this.ThrowExecutionError(
                            "No region specified or obtained from persisted/shell defaults.",
                            this,
                            null);
                        return null;
                    }

                    this._RegionEndpoint = RegionEndpoint.GetBySystemName(this._DefaultRegion);
                }
            }

            this.Context = new PSCloudFormationContext
            {
                Region = this._RegionEndpoint,
                Credentials = credentials,
                S3EndpointUrl = this.S3EndpointUrl,
                STSEndpointUrl = this.STSEndpointUrl,
                Logger = this.Logger
            };

            this._ClientFactory = new PSAwsClientFactory(
                this.CreateClient(this._CurrentCredentials, this._RegionEndpoint),
                this.Context);
            this.Context.S3Util = new S3Util(this._ClientFactory, this.Context);

            return this.Context;
        }

        /// <summary>
        /// Customizes the client configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        protected virtual void CustomizeClientConfig(ClientConfig config)
        {
            if (this.ParameterWasBound("EndpointUrl") && !string.IsNullOrEmpty(this.EndpointUrl))
            {
                config.AuthenticationRegion = config.RegionEndpoint.SystemName;
                config.ServiceURL = this.EndpointUrl;
            }
        }

        /// <summary>
        /// Tests whether the named parameter was bound.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns><c>true</c> if parameter bound; else <c>false</c></returns>
        protected bool ParameterWasBound(string parameterName)
        {
            return this.MyInvocation.BoundParameters.ContainsKey(parameterName);
        }

        /// <summary>
        /// Cmdlet end processing - dispose resources.
        /// </summary>
        protected override void EndProcessing()
        {
            base.EndProcessing();
            this._ClientFactory?.Dispose();

            (this.Context.S3Util as S3Util)?.Dispose();
        }

        /// <summary>
        /// Helper to throw an error occurring during service execution
        /// </summary>
        /// <param name="message">The message to emit to the error record, if <paramref name="exception"/> is <c>null</c></param>
        /// <param name="errorSource">The source (parameter or cmdlet) reporting the error</param>
        /// <param name="exception">The exception that was caught, if any</param>
        protected void ThrowExecutionError(string message, object errorSource, Exception exception)
        {
            if (this.HasDebugSwitch())
            {
                this.Logger.LogDebug(string.Join("\n", GetDebugAssemblyDetails()));
            }

            if (exception == null)
            {
                this.ThrowTerminatingError(
                    new ErrorRecord(
                        new InvalidOperationException(message),
                        "InvalidOperationException",
                        ErrorCategory.InvalidOperation,
                        errorSource));
            }

            new ExceptionDumper(
                this.Host.UI.WriteLine,
                this.MyInvocation.BoundParameters.Keys.Any(
                    k => string.Compare(k, "Debug", StringComparison.OrdinalIgnoreCase) == 0)).Dump(exception);

            var stackOperationException = exception.FindInner<StackOperationException>();

            if (stackOperationException != null)
            {
                var stackOperationToErrorCategory = new Dictionary<StackOperationalState, ErrorCategory>
                                                        {
                                                            { StackOperationalState.Busy, ErrorCategory.ResourceBusy },
                                                            {
                                                                StackOperationalState.Ready,
                                                                ErrorCategory.InvalidOperation
                                                            },
                                                            {
                                                                StackOperationalState.NotFound,
                                                                ErrorCategory.ObjectNotFound
                                                            },
                                                            {
                                                                StackOperationalState.Deleting,
                                                                ErrorCategory.ResourceBusy
                                                            },
                                                            {
                                                                StackOperationalState.DeleteFailed,
                                                                ErrorCategory.InvalidOperation
                                                            },
                                                            {
                                                                StackOperationalState.Unknown,
                                                                ErrorCategory.InvalidOperation
                                                            },
                                                            {
                                                                StackOperationalState.Broken,
                                                                ErrorCategory.InvalidOperation
                                                            },
                                                            {
                                                                StackOperationalState.Exists,
                                                                ErrorCategory.ResourceExists
                                                            }
                                                        };

                this.ThrowTerminatingError(
                    new ErrorRecord(
                        stackOperationException,
                        stackOperationException.GetType().ToString(),
                        stackOperationToErrorCategory[stackOperationException.OperationalState],
                        errorSource));
            }
            else
            {
                var resolvedException = exception is AggregateException aex ? aex.InnerExceptions.First() : exception;

                this.ThrowTerminatingError(
                    new ErrorRecord(
                        resolvedException,
                        // ReSharper disable once PossibleNullReferenceException - aggregates always have at least one inner exception
                        resolvedException.GetType().ToString(),
                        ErrorCategory.InvalidOperation,
                        errorSource));
            }
        }

        /// <summary>
        /// Determines whether [has debug switch].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [has debug switch]; otherwise, <c>false</c>.
        /// </returns>
        protected bool HasDebugSwitch()
        {
            return this.MyInvocation.BoundParameters.Keys.Any(
                k => string.Compare(k, "Debug", StringComparison.OrdinalIgnoreCase) == 0);
        }

        /// <summary>
        /// Gets the identities of relevant loaded assemblies for debug output on exception.
        /// </summary>
        /// <returns>List of loaded assemblies</returns>
        private static IEnumerable<string> GetDebugAssemblyDetails()
        {
            return new[] { "Relevant Assemblies:" }.Concat(
                (from assembly in AppDomain.CurrentDomain.GetAssemblies().Where(
                     a => a.FullName.StartsWith("AWS.") || a.FullName.StartsWith("AWSSDK.")
                                                        || a.FullName.StartsWith("Firefly."))
                 let name = assembly.FullName
                 let fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion
                 select $"{name}, FileVersion={fileVersion}")
                .OrderBy(s => s));
        }
    }
}