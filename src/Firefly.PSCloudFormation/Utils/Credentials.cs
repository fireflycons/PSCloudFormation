/*******************************************************************************
 *  Copyright 2012-2018 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *  Licensed under the Apache License, Version 2.0 (the "License"). You may not use
 *  this file except in compliance with the License. A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 *  or in the "license" file accompanying this file.
 *  This file is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
 *  CONDITIONS OF ANY KIND, either express or implied. See the License for the
 *  specific language governing permissions and limitations under the License.
 * *****************************************************************************
 *
 *  AWS Tools for Windows (TM) PowerShell (TM)
 *
 */

/*
 * This file is 99% functionally unchanged from the version in AWS Tools repo at time of copying.
 * It has had a code-tidy. plus a helper method to acquire session credentials using reflection
 * due to type mismatch with classes in AWS.Tools.Common
 *
 * I need these classes in here to remove the dependency on AWS.Tools.Common resulting in
 * this module only running against a specific version of AWS.Tools
 */

#pragma warning disable 1591
// ReSharper disable StyleCop.SA1600
// ReSharper disable StyleCop.SA1201
// ReSharper disable InconsistentNaming
// ReSharper disable StyleCop.SA1602
// ReSharper disable StyleCop.SA1503
// ReSharper disable StyleCop.SA1402
// ReSharper disable StyleCop.SA1305
// ReSharper disable once CheckNamespace
namespace Amazon.PowerShell.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Net;
    using System.Text;

    using Amazon.Runtime;
    using Amazon.Runtime.CredentialManagement;
    using Amazon.Util;

    #region Credentials arguments

    public enum CredentialsSource
    {
        Strings,

        Profile,

        CredentialsObject,

        Session,

        Environment,

        Container,

        InstanceProfile,

        Unknown
    }

    internal interface IAWSCredentialsArguments
    {
        string AccessKey { get; }

        AWSCredentials Credential { get; }

        PSCredential NetworkCredential { get; }

        string ProfileLocation { get; }

        string ProfileName { get; }

        string SecretKey { get; }

        string SessionToken { get; }

        CredentialProfileOptions GetCredentialProfileOptions();
    }

    internal interface IAWSCredentialsArgumentsFull : IAWSCredentialsArguments
    {
        string ExternalID { get; }

        string MfaSerial { get; }

        string RoleArn { get; }

        string SourceProfile { get; }
    }

    /// <summary>
    /// Wrapper around a set of AWSCredentials (various leaf types) carrying credential data,
    /// logical name and source info. $StoredAWSCredentials points to an instance of this and
    /// the ToString() override allows us to display more useful info (the set name) than
    /// what AWSCredentials on its own can at present.
    /// </summary>
    public class AWSPSCredentials
    {
        internal AWSPSCredentials(AWSCredentials credentials, string name, CredentialsSource source)
        {
            this.Credentials = credentials;
            this.Name = name;
            this.Source = source;
        }

        private AWSPSCredentials()
        {
        }

        internal AWSCredentials Credentials { get; private set; }

        internal string Name { get; private set; }

        internal CredentialsSource Source { get; private set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(this.Name))
                return this.Name;
            return this.Credentials != null ? this.Credentials.ToString() : base.ToString();
        }
    }

    /// <summary>
    /// Performs a search amongst a chain of credential parameters and provider methods to
    /// arrive at at set of AWS credentials.
    /// </summary>
    internal static class ICredentialsArgumentsMethods
    {
        public static bool TryGetCredentials(
            this IAWSCredentialsArguments self,
            PSHost psHost,
            out AWSPSCredentials credentials,
            SessionState sessionState)
        {
            if (self == null)
            {
                throw new ArgumentNullException("self");
            }

            credentials = null;
            string name = null;
            var source = CredentialsSource.Unknown;
            var userSpecifiedProfile = !string.IsNullOrEmpty(self.ProfileName);

            var profileChain = new CredentialProfileStoreChain(self.ProfileLocation);

            // we probe for credentials by first checking the bound parameters to see if explicit credentials 
            // were supplied (keys, profile name, credential object), overriding anything in the shell environment
            if (AWSCredentialsFactory.TryGetAWSCredentials(
                self.GetCredentialProfileOptions(),
                profileChain,
                out var innerCredentials))
            {
                source = CredentialsSource.Strings;
                name = "Supplied Key Parameters";
                SetProxyAndCallbackIfNecessary(innerCredentials, self, psHost, sessionState);
            }

            // user gave us the profile name?
            if (innerCredentials == null && userSpecifiedProfile)
            {
                if (profileChain.TryGetProfile(self.ProfileName, out var credentialProfile))
                {
                    innerCredentials = AWSCredentialsFactory.GetAWSCredentials(credentialProfile, profileChain);
                    source = CredentialsSource.Profile;
                    name = self.ProfileName;
                    SetProxyAndCallbackIfNecessary(innerCredentials, self, psHost, sessionState);
                }
                else
                {
                    // if the user gave us an explicit profile name (and optional location) it's an error if we
                    // don't find it as otherwise we could drop through and pick up a 'default' profile that is
                    // for a different account
                    return false;
                }
            }

            // how about an aws credentials object?
            if (innerCredentials == null && self.Credential != null)
            {
                innerCredentials = self.Credential;
                source = CredentialsSource.CredentialsObject;
                name = "Credentials Object";

                // don't set proxy and callback, use self.Credential as-is
            }

            // shell session variable set (this allows override of machine-wide environment variables)
            if (innerCredentials == null && sessionState != null)
            {
                if (TryGetAWSPSCredentialsFromConflictingType(
                    sessionState.PSVariable.GetValue(SessionKeys.AWSCredentialsVariableName),
                    out var psCredentials))
                {
                    credentials = psCredentials;
                    source = CredentialsSource.Session;
                    innerCredentials = credentials.Credentials; // so remaining probes are skipped

                    // don't set proxy and callback, use credentials.Credentials as-is
                }
            }

            // no explicit command-level or shell instance override set, start to inspect the environment
            // starting environment variables
            if (innerCredentials == null)
            {
                try
                {
                    var environmentCredentials = new EnvironmentVariablesAWSCredentials();
                    innerCredentials = environmentCredentials;
                    source = CredentialsSource.Environment;
                    name = "Environment Variables";

                    // no need to set proxy and callback - only basic or session credentials
                }
                catch
                {
                }
            }

            // get credentials from a 'default' profile?
            if (innerCredentials == null && !userSpecifiedProfile)
            {
                if (profileChain.TryGetProfile(SettingsStore.PSDefaultSettingName, out var credentialProfile)
                    && credentialProfile.CanCreateAWSCredentials)
                {
                    innerCredentials = AWSCredentialsFactory.GetAWSCredentials(credentialProfile, profileChain);
                    source = CredentialsSource.Profile;
                    name = SettingsStore.PSDefaultSettingName;
                    SetProxyAndCallbackIfNecessary(innerCredentials, self, psHost, sessionState);
                }
            }

            // get credentials from a legacy default profile name?
            if (innerCredentials == null)
            {
                if (profileChain.TryGetProfile(SettingsStore.PSLegacyDefaultSettingName, out var credentialProfile)
                    && credentialProfile.CanCreateAWSCredentials)
                {
                    if (AWSCredentialsFactory.TryGetAWSCredentials(
                        credentialProfile,
                        profileChain,
                        out innerCredentials))
                    {
                        source = CredentialsSource.Profile;
                        name = SettingsStore.PSLegacyDefaultSettingName;
                        SetProxyAndCallbackIfNecessary(innerCredentials, self, psHost, sessionState);
                    }
                }
            }

            if (innerCredentials == null)
            {
                // try and load credentials from ECS endpoint (if the relevant environment variable is set)
                // or EC2 Instance Profile as a last resort
                try
                {
                    string relativeUri =
                        Environment.GetEnvironmentVariable(ECSTaskCredentials.ContainerCredentialsURIEnvVariable);
                    string fullUri = Environment.GetEnvironmentVariable(
                        ECSTaskCredentials.ContainerCredentialsFullURIEnvVariable);

                    if (!string.IsNullOrEmpty(relativeUri) || !string.IsNullOrEmpty(fullUri))
                    {
                        innerCredentials = new ECSTaskCredentials();
                        source = CredentialsSource.Container;
                        name = "Container";

                        // no need to set proxy and callback
                    }
                    else
                    {
                        innerCredentials = new InstanceProfileAWSCredentials();
                        source = CredentialsSource.InstanceProfile;
                        name = "Instance Profile";

                        // no need to set proxy and callback
                    }
                }
                catch
                {
                    innerCredentials = null;
                }
            }

            if (credentials == null && innerCredentials != null)
            {
                credentials = new AWSPSCredentials(innerCredentials, name, source);
            }

            return credentials != null;
        }

        /// <summary>
        /// Try to obtain an <see cref="AWSCredentials"/> from the PowerShell session variable <c>StoredAWSCredentials</c> by reflection.
        /// Because we have duplicated some of the credential classes we have conflicting types, therefore we have to get to the underlying
        /// common credentials using reflection.
        /// </summary>
        /// <param name="sessionVariableValue">The session variable value.</param>
        /// <param name="credentials">The credentials.</param>
        /// <returns><c>true</c> if credentials obtained from PowerShell session; else <c>false</c>.</returns>
        private static bool TryGetAWSPSCredentialsFromConflictingType(object sessionVariableValue, out AWSPSCredentials credentials)
        {
            credentials = null;

            if (sessionVariableValue == null)
            {
                return false;
            }

            var properties = sessionVariableValue.GetType().GetProperties(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var credentialsProperty =
                properties.FirstOrDefault(p => typeof(AWSCredentials).IsAssignableFrom(p.PropertyType));

            var nameProperty = properties.FirstOrDefault(p => p.Name == "Name" && p.PropertyType == typeof(string));

            if (credentialsProperty != null)
            {
                credentials = new AWSPSCredentials(
                    (AWSCredentials)credentialsProperty.GetValue(sessionVariableValue),
                    nameProperty != null ? (string)nameProperty.GetValue(sessionVariableValue) : "none",
                    CredentialsSource.Session);
                return true;
            }

            return false;
        }

        private static WebProxy GetWebProxy(IAWSCredentialsArguments self, SessionState sessionState)
        {
            var proxySettings = ProxySettings.GetFromSettingsVariable(sessionState);
            return proxySettings != null ? proxySettings.GetWebProxy() : null;
        }

        private static string ReadMFACode()
        {
            Console.Write("Enter MFA code:");

            string mfaCode = string.Empty;
            while (true)
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
                if (info.Key == ConsoleKey.Backspace)
                {
                    if (mfaCode.Length > 0)
                    {
                        // remove the character from the string
                        mfaCode = mfaCode.Remove(mfaCode.Length - 1);

                        // remove the * from the console
                        var position = Console.CursorLeft - 1;
                        Console.SetCursorPosition(position, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(position, Console.CursorTop);
                    }
                }
                else if (info.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else
                {
                    mfaCode += info.KeyChar;
                    Console.Write("*");
                }
            }

            return mfaCode;
        }

        private static void SetProxyAndCallbackIfNecessary(
            AWSCredentials innerCredentials,
            IAWSCredentialsArguments self,
            PSHost psHost,
            SessionState sessionState)
        {
            SetupIfFederatedCredentials(innerCredentials, psHost, self, sessionState);
            SetupIfAssumeRoleCredentials(innerCredentials, self, sessionState);
        }

        private static void SetupIfAssumeRoleCredentials(
            AWSCredentials credentials,
            IAWSCredentialsArguments self,
            SessionState sessionState)
        {
            var assumeRoleCredentials = credentials as AssumeRoleAWSCredentials;
            if (assumeRoleCredentials != null)
            {
                // set up callback
                assumeRoleCredentials.Options.MfaTokenCodeCallback = ReadMFACode;

                // set up proxy
                assumeRoleCredentials.Options.ProxySettings = GetWebProxy(self, sessionState);
            }
        }

        private static void SetupIfFederatedCredentials(
            AWSCredentials credentials,
            PSHost psHost,
            IAWSCredentialsArguments self,
            SessionState sessionState)
        {
            // if we have picked up a SAML-based credentials profile, make sure the callback
            // to authenticate the user is set. The underlying SDK will then call us back
            // if it needs to (we could skip setting if the profile indicates its for the
            // default identity, but it's simpler to just set up anyway)
            var samlCredentials = credentials as FederatedAWSCredentials;
            if (samlCredentials != null)
            {
                // set up callback
                var state = new SAMLCredentialCallbackState
                                {
                                    Host = psHost, CmdletNetworkCredentialParameter = self.NetworkCredential
                                };
                samlCredentials.Options.CredentialRequestCallback = UserCredentialCallbackHandler;
                samlCredentials.Options.CustomCallbackState = state;

                // set up proxy
                samlCredentials.Options.ProxySettings = GetWebProxy(self, sessionState);
            }
        }

        private static NetworkCredential UserCredentialCallbackHandler(CredentialRequestCallbackArgs args)
        {
            var callbackContext = args.CustomState as SAMLCredentialCallbackState;
            if (callbackContext == null) // not our callback, so don't attempt to handle
                return null;

            // if we are not retrying due to auth failure, did the user pre-supply a credential
            // via the -NetworkCredential parameter to either the cmdlet or Set-AWSCredentials?
            PSCredential psCredential = null;
            string msgPrompt = null;
            if (!args.PreviousAuthenticationFailed)
            {
                if (callbackContext.CmdletNetworkCredentialParameter != null)
                {
                    psCredential = callbackContext.CmdletNetworkCredentialParameter;
                    callbackContext.CmdletNetworkCredentialParameter = null; // the cmdlet override is single use
                }
                else if (callbackContext.ShellNetworkCredentialParameter != null)
                    psCredential = callbackContext.ShellNetworkCredentialParameter;
                else
                    msgPrompt = string.Format(
                        "Enter your credentials to authenticate and obtain AWS role credentials for the profile '{0}'",
                        args.ProfileName);
            }
            else
                msgPrompt = string.Format(
                    "Authentication failed. Enter the password for '{0}' to try again.",
                    args.UserIdentity);

            // some profiles have a user identity expressed in email terms with a mishandled domain, leading to
            // identity of \me@domain.com - the user then has to strip the \, so let's do it for them
            var userIdentity = string.IsNullOrEmpty(args.UserIdentity) ? null : args.UserIdentity.TrimStart('\\');
            if (psCredential == null)
                psCredential = callbackContext.Host.UI.PromptForCredential(
                    "Authenticating for AWS Role Credentials",
                    msgPrompt,
                    userIdentity,
                    string.Empty);

            return psCredential != null ? psCredential.GetNetworkCredential() : null;
        }
    }

    /// <summary>
    /// Captures the PSHost and executing cmdlet state for use in our credential callback
    /// handler.
    /// </summary>
    internal class SAMLCredentialCallbackState
    {
        /// <summary>
        /// Any PSCredential argument supplied to the current cmdlet invocation.
        /// This overrides ShellNetworkCredentialParameter that may have been set 
        /// in the shell when Set-AWSCredentials was invoked. The value is cleared
        /// after use.
        /// </summary>
        public PSCredential CmdletNetworkCredentialParameter { get; set; }

        /// <summary>
        /// The execution host, used to display credential prompts
        /// </summary>
        public PSHost Host { get; set; }

        /// <summary>
        /// Null or the value of the NetworkCredential parameter that was supplied
        /// when the role profile was set active in the shell via Set-AWSCredentials.
        /// If set, this credential is used if a more local scope credential cannot
        /// be found in SelfNetworkCredentialParameter. This value is retained after 
        /// use.
        /// </summary>
        public PSCredential ShellNetworkCredentialParameter { get; set; }
    }

    #endregion

    #region Region arguments

    public enum RegionSource
    {
        String,

        Saved,

        RegionObject,

        Session,

        Environment,

        InstanceMetadata,

        Unknown
    }

    internal interface IAWSRegionArguments
    {
        string ProfileLocation { get; }

        object Region { get; }
    }

    internal static class IAWSRegionArgumentsMethods
    {
        public static RegionEndpoint GetRegion(
            this IAWSRegionArguments self,
            bool useSDKFallback,
            SessionState sessionState)
        {
            if (!TryGetRegion(self, useSDKFallback, out var region, out var source, sessionState))
                region = null;
            return region;
        }

        public static bool TryGetRegion(
            this IAWSRegionArguments self,
            bool useInstanceMetadata,
            out RegionEndpoint region,
            out RegionSource source,
            SessionState sessionState)
        {
            if (self == null) throw new ArgumentNullException("self");

            region = null;
            source = RegionSource.Unknown;

            // user gave a command-level region parameter override?
            if (self.Region != null)
            {
                string regionSysName = string.Empty;
                if (self.Region is PSObject)
                {
                    PSObject paramObject = self.Region as PSObject;
                    if (paramObject.BaseObject is AWSRegion)
                        regionSysName = (paramObject.BaseObject as AWSRegion).Region.SystemName;
                    else
                        regionSysName = paramObject.BaseObject as string;
                }
                else if (self.Region is string)
                    regionSysName = self.Region as string;

                if (string.IsNullOrEmpty(regionSysName))
                    throw new ArgumentException(
                        "Unsupported parameter type; Region must be a string containing the system name for a region, or an AWSRegion instance");

                try
                {
                    region = RegionEndpoint.GetBySystemName(regionSysName);
                    source = RegionSource.String;
                }
                catch (Exception)
                {
                    // be nice and informative :-)
                    StringBuilder sb = new StringBuilder("Unsupported Region value. Supported values: ");
                    var regions = RegionEndpoint.EnumerableAllRegions;
                    for (int i = 0; i < regions.Count(); i++)
                    {
                        if (i > 0) sb.Append(",");
                        sb.Append(regions.ElementAt(i).SystemName);
                    }

                    throw new ArgumentOutOfRangeException(sb.ToString());
                }
            }

            // user pushed default shell variable? (this allows override of machine-wide environment setting)
            if (region == null && sessionState != null)
            {
                object variableValue = sessionState.PSVariable.GetValue(SessionKeys.AWSRegionVariableName);
                if (variableValue is string)
                {
                    region = RegionEndpoint.GetBySystemName(variableValue as string);
                    source = RegionSource.Session;
                }
            }

            // region set in profile store (including legacy key name)?
            if (region == null)
            {
                if (!TryLoad(SettingsStore.PSDefaultSettingName, self.ProfileLocation, ref region, ref source))
                    TryLoad(SettingsStore.PSLegacyDefaultSettingName, self.ProfileLocation, ref region, ref source);
            }

            // region set in environment variables?
            if (region == null)
            {
                try
                {
                    var environmentRegion = new EnvironmentVariableAWSRegion();
                    region = environmentRegion.Region;
                    source = RegionSource.Environment;
                }
                catch
                {
                }
            }

            // last chance, attempt load from EC2 instance metadata if allowed
            if (region == null && useInstanceMetadata)
            {
                try
                {
                    region = EC2InstanceMetadata.Region;
                    if (region != null)
                        source = RegionSource.InstanceMetadata;
                }
                catch
                {
                }
            }

            return (region != null && source != RegionSource.Unknown);
        }

        private static bool TryLoad(
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
    }

    #endregion

    #region Helper utils

    public static class SettingsStore
    {
        public const string PSDefaultSettingName = "default";

        public const string PSLegacyDefaultSettingName = "AWS PS Default";

        public static List<ProfileInfo> GetProfileInfo(string profileLocation)
        {
            var profiles = (new CredentialProfileStoreChain(profileLocation)).ListProfiles();
            var result = new List<ProfileInfo>();
            foreach (var profile in profiles)
            {
                string location = null;
                var sharedCredentialsFile = profile.CredentialProfileStore as SharedCredentialsFile;
                if (sharedCredentialsFile == null)
                {
                    var netsSDKCredentialsFile = profile.CredentialProfileStore as NetSDKCredentialsFile;
                    if (netsSDKCredentialsFile != null)
                    {
                        location = null;
                    }
                }
                else
                {
                    location = sharedCredentialsFile.FilePath;
                }

                result.Add(
                    new ProfileInfo
                        {
                            ProfileLocation = location,
                            ProfileName = profile.Name,
                            StoreTypeName = profile.CredentialProfileStore.GetType().Name
                        });
            }

            return result;
        }

        public static IEnumerable<CredentialProfile> ListProfiles(string profileLocation)
        {
            return new CredentialProfileStoreChain(profileLocation).ListProfiles();
        }

        public static bool ProfileExists(string name, string profileLocation)
        {
            return GetProfileInfo(profileLocation)
                .Any(pi => string.Equals(pi.ProfileName, name, StringComparison.Ordinal));
        }

        public static void RegisterProfile(
            CredentialProfileOptions profileOptions,
            string name,
            string profileLocation,
            RegionEndpoint region)
        {
            var profile = new CredentialProfile(name, profileOptions);
            profile.Region = region;
            new CredentialProfileStoreChain(profileLocation).RegisterProfile(profile);
        }

        public static bool TryGetAWSCredentials(string name, string profileLocation, out AWSCredentials credentials)
        {
            return new CredentialProfileStoreChain(profileLocation).TryGetAWSCredentials(name, out credentials);
        }

        public static bool TryGetProfile(string name, string profileLocation, out CredentialProfile profile)
        {
            return new CredentialProfileStoreChain(profileLocation).TryGetProfile(name, out profile);
        }

        public static void UnregisterProfile(string name, string profileLocation)
        {
            new CredentialProfileStoreChain(profileLocation).UnregisterProfile(name);
        }
    }

    internal static class SessionKeys
    {
        public const string AWSCallHistoryName = "AWSHistory";

        public const string AWSCredentialsVariableName = "StoredAWSCredentials";

        public const string AWSProxyVariableName = "AWSProxy";

        public const string AWSRegionVariableName = "StoredAWSRegion";
    }

    internal static class CredentialProfileOptionsExtractor
    {
        public static CredentialProfileOptions ExtractProfileOptions(AWSCredentials credentials)
        {
            var type = credentials.GetType();
            if (type == typeof(BasicAWSCredentials) || type == typeof(SessionAWSCredentials))
            {
                var immutableCredentials = credentials.GetCredentials();
                return new CredentialProfileOptions
                           {
                               AccessKey = immutableCredentials.AccessKey,
                               SecretKey = immutableCredentials.SecretKey,
                               Token = immutableCredentials.Token
                           };
            }

            if (PassThroughExtractTypes.Contains(type))
                return null;

            if (ThrowExtractTypes.Contains(type))
                throw new InvalidOperationException("Cannot save credentials of type " + type.Name);

            throw new InvalidOperationException("Unrecognized credentials type: " + type.Name);
        }

#pragma warning disable CS0618 // A class was marked with the Obsolete attribute
        private static HashSet<Type> PassThroughExtractTypes = new HashSet<Type>
                                                                   {
                                                                       typeof(InstanceProfileAWSCredentials),
#if DESKTOP
            typeof(StoredProfileFederatedCredentials),
#endif
                                                                       typeof(FederatedAWSCredentials),
                                                                   };

        private static HashSet<Type> ThrowExtractTypes = new HashSet<Type>
                                                             {
                                                                 typeof(AssumeRoleAWSCredentials),
                                                                 typeof(URIBasedRefreshingCredentialHelper),
                                                                 typeof(AnonymousAWSCredentials),
                                                                 typeof(ECSTaskCredentials),
                                                                 typeof(EnvironmentVariablesAWSCredentials),
                                                                 typeof(StoredProfileAWSCredentials),
#if DESKTOP
            typeof(EnvironmentAWSCredentials),
#endif
                                                                 typeof(EnvironmentVariablesAWSCredentials)
                                                             };
#pragma warning restore CS0618 // A class was marked with the Obsolete attribute
    }

    public class ProfileInfo
    {
        public string ProfileLocation { get; set; }

        public string ProfileName { get; set; }

        public string StoreTypeName { get; set; }
    }

    /// <summary>
    /// Proxy settings for AWS cmdlets
    /// </summary>
    public class ProxySettings
    {
        /// <summary>
        /// A collection of regular expressions denoting the set of endpoints for
        /// which the configured proxy host will be bypassed.
        /// </summary>
        /// <remarks>
        ///  For more information on bypass lists 
        ///  see https://msdn.microsoft.com/en-us/library/system.net.webproxy.bypasslist%28v=vs.110%29.aspx.
        /// </remarks>
        public List<string> BypassList { get; set; }

        /// <summary>
        /// If set true requests to local addresses bypass the configured proxy.
        /// </summary>
        public bool? BypassOnLocal { get; set; }

        /// <summary>
        /// Proxy credentials
        /// </summary>
        public ICredentials Credentials { get; set; }

        /// <summary>
        /// Proxy host
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Proxy port
        /// </summary>
        public int Port { get; set; }

        internal bool UseProxy
        {
            get
            {
                return !string.IsNullOrEmpty(this.Hostname) && this.Port != 0;
            }
        }

        internal static ProxySettings GetFromSettingsVariable(SessionState session)
        {
            ProxySettings ps = null;

            var variable = session.PSVariable.Get(SessionKeys.AWSProxyVariableName);
            if (variable != null && variable.Value != null)
                ps = variable.Value as ProxySettings;

            return ps;
        }

        internal static ProxySettings GetSettings(PSCmdlet cmdlet)
        {
            ProxySettings ps = GetFromSettingsVariable(cmdlet.SessionState);
            if (ps == null)
                ps = new ProxySettings();

            return ps;
        }

        internal WebProxy GetWebProxy()
        {
            const string httpPrefix = "http://";

            WebProxy proxy = null;
            if (!string.IsNullOrEmpty(this.Hostname) && this.Port > 0)
            {
                // WebProxy constructor adds the http:// prefix, but doesn't
                // account for cases where it's already present which leads to
                // malformed addresses
                var host = this.Hostname.StartsWith(httpPrefix, StringComparison.OrdinalIgnoreCase)
                               ? this.Hostname.Substring(httpPrefix.Length)
                               : this.Hostname;
                proxy = new WebProxy(host, this.Port);

                if (this.Credentials != null)
                {
                    proxy.Credentials = this.Credentials;
                }

                if (this.BypassList != null)
                {
                    proxy.BypassList = this.BypassList.ToArray();
                }

                if (this.BypassOnLocal.HasValue)
                    proxy.BypassProxyOnLocal = this.BypassOnLocal.Value;
            }

            return proxy;
        }
    }

#endregion
}