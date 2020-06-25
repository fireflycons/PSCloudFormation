namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Net;

    public enum VariableScope
    {
        Global = -1,

        Local = -2,

        Script = -3,

        Private = -4
    }

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
                if (!string.IsNullOrEmpty(this.Hostname))
                {
                    return this.Port != 0;
                }

                return false;
            }
        }

        internal static ProxySettings GetFromSettingsVariable(SessionState session)
        {
            ProxySettings result = null;
            PSVariable pSVariable = session.PSVariable.Get("AWSProxy");
            if (pSVariable != null && pSVariable.Value != null)
            {
                result = (pSVariable.Value as ProxySettings);
            }

            return result;
        }

        internal static ProxySettings GetSettings(PSCmdlet cmdlet)
        {
            ProxySettings proxySettings = GetFromSettingsVariable(cmdlet.SessionState);
            if (proxySettings == null)
            {
                proxySettings = new ProxySettings();
            }

            return proxySettings;
        }

        internal static void SaveSettings(PSCmdlet cmdlet, ProxySettings settings, VariableScope? variableScope)
        {
            string str = variableScope.HasValue ? (variableScope.Value.ToString() + ":") : string.Empty;
            cmdlet.SessionState.PSVariable.Set(str + "AWSProxy", settings);
        }

        internal WebProxy GetWebProxy()
        {
            WebProxy webProxy = null;
            if (!string.IsNullOrEmpty(this.Hostname) && this.Port > 0)
            {
                webProxy = new WebProxy(
                    this.Hostname.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        ? this.Hostname.Substring("http://".Length)
                        : this.Hostname,
                    this.Port);
                if (this.Credentials != null)
                {
                    webProxy.Credentials = this.Credentials;
                }

                if (this.BypassList != null)
                {
                    webProxy.BypassList = this.BypassList.ToArray();
                }

                if (this.BypassOnLocal.HasValue)
                {
                    webProxy.BypassProxyOnLocal = this.BypassOnLocal.Value;
                }
            }

            return webProxy;
        }

        internal void SaveSettings(PSCmdlet cmdlet, VariableScope? variableScope)
        {
            SaveSettings(cmdlet, this, variableScope);
        }
    }
}