// ReSharper disable StyleCop.SA1126
// ReSharper disable StyleCop.SA1305
// ReSharper disable InconsistentNaming

namespace Firefly.PSCloudFormation
{
    using System;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Net;
    using System.Reflection;

    using Amazon;
    using Amazon.CloudFormation;
    using Amazon.PowerShell.Common;
    using Amazon.PowerShell.Utils;
    using Amazon.Runtime;
    using Amazon.Runtime.CredentialManagement;
    using Amazon.Util;

    using Firefly.CloudFormation;

    using AWSRegion = Amazon.PowerShell.Common.AWSRegion;

    /// <summary>
    /// <para>
    /// Service level base class. Contains much of the implementation for determining credentials and region from AWS.Tools.Common
    /// This was re-implemented here rather than just inheriting from AWS <see cref="ServiceCmdlet"/> as they don't do this evaluation until the pipeline is being processed.
    /// We need this information earlier in order to be able to process dynamic parameters when updating with Use Previous Template, i.e. we need to be able to retrieve
    /// existing template from CloudFormation.
    /// </para>
    /// <para>
    /// WARNING: May cause issues if the credential arguments are being read from an object in the pipeline, as in the object may not yet be resolved.
    /// </para>
    /// </summary>
    /// <seealso cref="System.Management.Automation.PSCmdlet" />
    public class CloudFormationServiceCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the access key.
        /// <para type="description">
        /// The AWS access key for the user account. This can be a temporary access key
        /// if the corresponding session token is supplied to the -SessionToken parameter.
        /// </para>
        /// </summary>
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
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public AWSCredentials Credential { get; set; }

        /// <summary>
        /// Gets or sets the endpoint URL.
        /// </summary>
        /// <value>
        /// The endpoint URL.
        /// </value>
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
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Alias("RegionToCall")]
        public object Region { get; set; }

        /// <summary>
        /// Gets or sets the s3 endpoint URL.
        /// <para type="description">
        /// The endpoint to make S3 calls against.Note: This parameter is primarily for internal AWS use and is not required/should not be specified for normal usage.
        /// The cmdlets normally determine which endpoint to call based on the region specified to the -Region parameter or set as default in the shell (via Set-DefaultAWSRegion).
        /// Only specify this parameter if you must direct the call to a specific custom endpoint.
        /// </para>
        /// </summary>
        /// <value>
        /// The s3 endpoint URL.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string S3EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the secret key
        /// <para type="description">
        /// The AWS secret key for the user account. This can be a temporary secret key
        /// if the corresponding session token is supplied to the -SessionToken parameter.
        /// </para>
        /// </summary>
        [Alias("SK", "SecretAccessKey")]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string SecretKey { get; set; }

        /// <summary>
        /// Gets or sets the session token
        /// <para type="description">
        /// The session token if the access and secret keys are temporary session-based credentials.
        /// </para>
        /// </summary>
        [Alias("ST")]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string SessionToken { get; set; }

        /// <summary>
        /// Gets or sets the STS endpoint URL.
        /// The endpoint to make STS calls against.Note: This parameter is primarily for internal AWS use and is not required/should not be specified for normal usage.
        /// The cmdlets normally determine which endpoint to call based on the region specified to the -Region parameter or set as default in the shell (via Set-DefaultAWSRegion).
        /// Only specify this parameter if you must direct the call to a specific custom endpoint.
        /// </summary>
        /// <value>
        /// The STS endpoint URL.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once StyleCop.SA1650
        public string STSEndpointUrl { get; set; }

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
        /// Gets or sets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        protected PSLogger Logger { get; set; }

        /// <summary>
        /// Gets the credential profile options.
        /// </summary>
        /// <returns>Access key credential options as a struct</returns>
        public CredentialProfileOptions GetCredentialProfileOptions()
        {
            return new CredentialProfileOptions
                       {
                           AccessKey = this.AccessKey, SecretKey = this.SecretKey, Token = this.SessionToken
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

            Common.PopulateConfig(this, amazonCloudFormationConfig);
            this.CustomizeClientConfig(amazonCloudFormationConfig);
            var amazonCloudFormationClient = new AmazonCloudFormationClient(credentials, amazonCloudFormationConfig);
            return amazonCloudFormationClient;
        }

        /// <summary>
        /// Creates the cloud formation context.
        /// </summary>
        /// <returns>New <see cref="ICloudFormationContext"/></returns>
        protected ICloudFormationContext CreateCloudFormationContext()
        {
            AWSCredentials credentials;

            if (this._CurrentCredentials != null)
            {
                credentials = this._CurrentCredentials;
            }
            else if (this.TryGetCredentials(this.Host, this.SessionState))
            {
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
                this.TryGetRegion(string.IsNullOrEmpty(this._DefaultRegion), out var region, this.SessionState);
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

            this.Logger = new PSLogger(this);

            return new PSCloudFormationContext
                       {
                           Region = this._RegionEndpoint,
                           Credentials = credentials,
                           S3EndpointUrl =
                               string.IsNullOrEmpty(this.S3EndpointUrl) ? null : new Uri(this.S3EndpointUrl),
                           STSEndpointUrl = string.IsNullOrEmpty(this.STSEndpointUrl)
                                                ? null
                                                : new Uri(this.S3EndpointUrl),
                           Logger = this.Logger
                       };
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
        /// Gets a web proxy.
        /// </summary>
        /// <param name="sessionState">State of the session.</param>
        /// <returns>A <see cref="WebProxy"/> object to use for calls.</returns>
        protected WebProxy GetWebProxy(SessionState sessionState)
        {
            return ProxySettings.GetFromSettingsVariable(sessionState)?.GetWebProxy();
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
        /// Asks for the user's MFA code.
        /// </summary>
        /// <returns>Code input by user.</returns>
        protected string ReadMFACode()
        {
            Console.Write("Enter MFA code:");
            var text = string.Empty;
            while (true)
            {
                var consoleKeyInfo = Console.ReadKey(true);
                if (consoleKeyInfo.Key == ConsoleKey.Backspace)
                {
                    if (text.Length > 0)
                    {
                        text = text.Remove(text.Length - 1);
                        var left = Console.CursorLeft - 1;
                        Console.SetCursorPosition(left, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(left, Console.CursorTop);
                    }
                }
                else
                {
                    if (consoleKeyInfo.Key == ConsoleKey.Enter)
                    {
                        break;
                    }

                    text += consoleKeyInfo.KeyChar.ToString();
                    Console.Write("*");
                }
            }

            Console.WriteLine();
            return text;
        }

        /// <summary>
        /// For credentials that require additional user input, set this up and perform it.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="psHost">The ps host.</param>
        /// <param name="sessionState">State of the session.</param>
        protected void SetProxyAndCallbackIfNecessary(
            AWSCredentials credentials,
            PSHost psHost,
            SessionState sessionState)
        {
            this.SetupIfFederatedCredentials(credentials, psHost, sessionState);
            this.SetupIfAssumeRoleCredentials(credentials, sessionState);
        }

        /// <summary>
        /// Do user interaction that may be required for assumed credentials
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="sessionState">State of the session.</param>
        protected void SetupIfAssumeRoleCredentials(AWSCredentials credentials, SessionState sessionState)
        {
            if (credentials is AssumeRoleAWSCredentials assumeRoleAWSCredentials)
            {
                assumeRoleAWSCredentials.Options.MfaTokenCodeCallback = this.ReadMFACode;
                assumeRoleAWSCredentials.Options.ProxySettings = this.GetWebProxy(sessionState);
            }
        }

        /// <summary>
        /// Do user interaction that may be required for federated credentials
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="psHost">The ps host.</param>
        /// <param name="sessionState">State of the session.</param>
        protected void SetupIfFederatedCredentials(AWSCredentials credentials, PSHost psHost, SessionState sessionState)
        {
            if (credentials is FederatedAWSCredentials federatedAWSCredentials)
            {
                var customCallbackState = new SAMLCredentialCallbackState
                                              {
                                                  Host = psHost,
                                                  CmdletNetworkCredentialParameter = this.NetworkCredential
                                              };
                federatedAWSCredentials.Options.CredentialRequestCallback = this.UserCredentialCallbackHandler;
                federatedAWSCredentials.Options.CustomCallbackState = customCallbackState;
                federatedAWSCredentials.Options.ProxySettings = this.GetWebProxy(sessionState);
            }
        }

        /// <summary>
        /// Helper to throw an error occuring during service execution
        /// </summary>
        /// <param name="message">The message to emit to the error record</param>
        /// <param name="errorSource">The source (parameter or cmdlet) reporting the error</param>
        /// <param name="innerException">The exception that was caught, if any</param>
        protected void ThrowExecutionError(string message, object errorSource, Exception innerException)
        {
            this.ThrowTerminatingError(
                new ErrorRecord(
                    new InvalidOperationException(message, innerException),
                    innerException == null ? "InvalidOperationException" : innerException.GetType().ToString(),
                    ErrorCategory.InvalidOperation,
                    errorSource));
        }

        /// <summary>
        /// Main call to determine AWS credentials given credential arguments on the command line and all other sources
        /// </summary>
        /// <param name="psHost">The PowerShell host.</param>
        /// <param name="sessionState">State of the session.</param>
        /// <returns><c>true</c> if credentials were found; else <c>false</c></returns>
        protected bool TryGetCredentials(PSHost psHost, SessionState sessionState)
        {
            var haveProfileName = !string.IsNullOrEmpty(this.ProfileName);

            var credentialProfileStoreChain = new CredentialProfileStoreChain(this.ProfileLocation);
            if (AWSCredentialsFactory.TryGetAWSCredentials(
                this.GetCredentialProfileOptions(),
                credentialProfileStoreChain,
                out var awsCredentials))
            {
                this.SetProxyAndCallbackIfNecessary(awsCredentials, psHost, sessionState);
            }

            if (awsCredentials == null && haveProfileName)
            {
                if (!credentialProfileStoreChain.TryGetProfile(this.ProfileName, out var profile))
                {
                    return false;
                }

                awsCredentials = AWSCredentialsFactory.GetAWSCredentials(profile, credentialProfileStoreChain);
                this.SetProxyAndCallbackIfNecessary(awsCredentials, psHost, sessionState);
            }

            if (awsCredentials == null && this.Credential != null)
            {
                awsCredentials = this.Credential;
            }

            if (awsCredentials == null && sessionState != null)
            {
                var storedCredentials = sessionState.PSVariable.GetValue("StoredAWSCredentials");

                if (storedCredentials is AWSPSCredentials awspsCredentials)
                {
                    awsCredentials = GetCredentialsFromPSCredentials(awspsCredentials);
                }
            }

            if (awsCredentials == null)
            {
                try
                {
                    // Will throw if environment variables not set
                    awsCredentials = new EnvironmentVariablesAWSCredentials();
                }
                catch
                {
                    // Swallow and continue
                }
            }

            if (awsCredentials == null && !haveProfileName
                                       && credentialProfileStoreChain.TryGetProfile("default", out var defaultProfile)
                                       && defaultProfile.CanCreateAWSCredentials)
            {
                awsCredentials = AWSCredentialsFactory.GetAWSCredentials(defaultProfile, credentialProfileStoreChain);
                this.SetProxyAndCallbackIfNecessary(awsCredentials, psHost, sessionState);
            }

            if (awsCredentials == null
                && credentialProfileStoreChain.TryGetProfile("AWS PS Default", out var defaultPsProfile)
                && defaultPsProfile.CanCreateAWSCredentials && AWSCredentialsFactory.TryGetAWSCredentials(
                    defaultPsProfile,
                    credentialProfileStoreChain,
                    out awsCredentials))
            {
                this.SetProxyAndCallbackIfNecessary(awsCredentials, psHost, sessionState);
            }

            if (awsCredentials == null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(
                            Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_RELATIVE_URI")))
                    {
                        awsCredentials = new ECSTaskCredentials();
                    }
                    else
                    {
                        awsCredentials = new InstanceProfileAWSCredentials();
                    }
                }
                catch
                {
                    awsCredentials = null;
                }
            }

            if (awsCredentials != null)
            {
                this._CurrentCredentials = awsCredentials;
            }

            return awsCredentials != null;
        }

        /// <summary>
        /// Try to determine user's current region from available sources
        /// </summary>
        /// <param name="useInstanceMetadata">if set to <c>true</c> [use instance metadata].</param>
        /// <param name="region">The region.</param>
        /// <param name="sessionState">State of the session.</param>
        /// <returns><c>true</c> if region was determined; else <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Unsupported parameter type; Region must be a string containing the system name for a region, or an AWSRegion instance</exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        protected bool TryGetRegion(bool useInstanceMetadata, out RegionEndpoint region, SessionState sessionState)
        {
            region = null;
            var source = RegionSource.Unknown;

            if (this.Region != null)
            {
                var regionSystemName = string.Empty;

                switch (this.Region)
                {
                    case PSObject psobject:
                        regionSystemName = !(psobject.BaseObject is AWSRegion)
                                               ? psobject.BaseObject as string
                                               : (psobject.BaseObject as AWSRegion)?.Region;
                        break;

                    case string s:
                        regionSystemName = s;
                        break;
                }

                if (string.IsNullOrEmpty(regionSystemName))
                {
                    throw new ArgumentException(
                        "Unsupported parameter type; Region must be a string containing the system name for a region, or an AWSRegion instance");
                }

                try
                {
                    region = RegionEndpoint.GetBySystemName(regionSystemName);
                    source = RegionSource.String;
                }
                catch (Exception ex)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Unsupported Region value. Supported values: {string.Join(", ", RegionEndpoint.EnumerableAllRegions)}",
                        ex);
                }
            }

            if (region == null && sessionState != null)
            {
                var value = sessionState.PSVariable.GetValue("StoredAWSRegion");

                if (value is string s)
                {
                    region = RegionEndpoint.GetBySystemName(s);
                    source = RegionSource.Session;
                }
            }

            if (region == null && !TryLoadRegionFromProfile("default", this.ProfileLocation, ref region, ref source))
            {
                TryLoadRegionFromProfile("AWS PS Default", this.ProfileLocation, ref region, ref source);
            }

            if (region == null)
            {
                try
                {
                    // Throws if environment variable undefined
                    var environmentVariableAWSRegion = new EnvironmentVariableAWSRegion();
                    region = environmentVariableAWSRegion.Region;
                    source = RegionSource.Environment;
                }
                catch
                {
                    // Do nothing
                }
            }

            if (region == null && useInstanceMetadata)
            {
                try
                {
                    region = EC2InstanceMetadata.Region;
                    if (region != null)
                    {
                        source = RegionSource.InstanceMetadata;
                    }
                }
                catch
                {
                    // Do nothing
                }
            }

            if (region != null)
            {
                return source != RegionSource.Unknown;
            }

            return false;
        }

        /// <summary>
        /// Callback handler for SAML authentication.
        /// </summary>
        /// <param name="callbackArguments">The callback arguments.</param>
        /// <returns>User's network credentials</returns>
        protected NetworkCredential UserCredentialCallbackHandler(CredentialRequestCallbackArgs callbackArguments)
        {
            if (!(callbackArguments.CustomState is SAMLCredentialCallbackState sAMLCredentialCallbackState))
            {
                return null;
            }

            PSCredential psCredential = null;
            string message = null;
            if (!callbackArguments.PreviousAuthenticationFailed)
            {
                if (sAMLCredentialCallbackState.CmdletNetworkCredentialParameter != null)
                {
                    psCredential = sAMLCredentialCallbackState.CmdletNetworkCredentialParameter;
                    sAMLCredentialCallbackState.CmdletNetworkCredentialParameter = null;
                }
                else if (sAMLCredentialCallbackState.ShellNetworkCredentialParameter != null)
                {
                    psCredential = sAMLCredentialCallbackState.ShellNetworkCredentialParameter;
                }
                else
                {
                    message =
                        $"Enter your credentials to authenticate and obtain AWS role credentials for the profile '{callbackArguments.ProfileName}'";
                }
            }
            else
            {
                message =
                    $"Authentication failed. Enter the password for '{callbackArguments.UserIdentity}' to try again.";
            }

            var userName = string.IsNullOrEmpty(callbackArguments.UserIdentity)
                               ? null
                               : callbackArguments.UserIdentity.TrimStart('\\');
            if (psCredential == null)
            {
                psCredential = sAMLCredentialCallbackState.Host.UI.PromptForCredential(
                    "Authenticating for AWS Role Credentials",
                    message,
                    userName,
                    string.Empty);
            }

            return psCredential?.GetNetworkCredential();
        }

        /// <summary>
        /// Helper to extract the value of the internal <c>Credentials</c> property of <see cref="AWSPSCredentials"/>
        /// </summary>
        /// <param name="psCredentials"><see cref="AWSPSCredentials"/> object.</param>
        /// <returns><see cref="AWSCredentials"/> value.</returns>
        /// <exception cref="InvalidOperationException">Property 'Credentials' not found on {typeof(AWSPSCredentials).Name}</exception>
        private static AWSCredentials GetCredentialsFromPSCredentials(AWSPSCredentials psCredentials)
        {
            var prop = typeof(AWSPSCredentials).GetProperty(
                "Credentials",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (prop != null)
            {
                return (AWSCredentials)prop.GetMethod.Invoke(psCredentials, null);
            }

            throw new InvalidOperationException($"Property 'Credentials' not found on {typeof(AWSPSCredentials).Name}");
        }

        /// <summary>
        /// Tries the load region from profile.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="profileLocation">The profile location.</param>
        /// <param name="region">The region.</param>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        private static bool TryLoadRegionFromProfile(
            string name,
            string profileLocation,
            ref RegionEndpoint region,
            ref RegionSource source)
        {
            if (SettingsStore.TryGetProfile(name, profileLocation, out var profile) && profile.Region != null)
            {
                region = profile.Region;
                source = RegionSource.Saved;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Captures the PSHost and executing cmdlet state for use in our credential callback
        /// handler.
        /// </summary>
        private class SAMLCredentialCallbackState
        {
            /// <summary>
            /// Gets or sets any PSCredential argument supplied to the current cmdlet invocation.
            /// This overrides ShellNetworkCredentialParameter that may have been set 
            /// in the shell when Set-AWSCredentials was invoked. The value is cleared
            /// after use.
            /// </summary>
            public PSCredential CmdletNetworkCredentialParameter { get; set; }

            /// <summary>
            /// Gets or sets the execution host, used to display credential prompts
            /// </summary>
            public PSHost Host { get; set; }

            /// <summary>
            /// Gets or sets the Shell Network Credential parameter
            /// </summary>
            /// <value>
            /// Null or the value of the NetworkCredential parameter that was supplied
            /// when the role profile was set active in the shell via Set-AWSCredentials.
            /// If set, this credential is used if a more local scope credential cannot
            /// be found in SelfNetworkCredentialParameter. This value is retained after 
            /// use.
            /// </value>
            public PSCredential ShellNetworkCredentialParameter { get; set; }
        }
    }
}