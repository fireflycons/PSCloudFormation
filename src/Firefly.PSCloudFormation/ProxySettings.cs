namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Net;

    /// <summary>
    /// PowerShell variable scopes
    /// </summary>
    public enum VariableScope
    {
        /// <summary>
        /// The global
        /// </summary>
        Global = -1,

        /// <summary>
        /// The local
        /// </summary>
        Local = -2,

        /// <summary>
        /// The script
        /// </summary>
        Script = -3,

        /// <summary>
        /// The private
        /// </summary>
        Private = -4
    }

    /// <summary>
    /// Settings for web proxies
    /// </summary>
    public class ProxySettings
    {
        /// <summary>
        /// Gets or sets a collection of regular expressions denoting the set of endpoints for
        /// which the configured proxy host will be bypassed.
        /// </summary>
        /// <remarks>
        ///  For more information on bypass lists 
        ///  <see href="https://msdn.microsoft.com/en-us/library/system.net.webproxy.bypasslist%28v=vs.110%29.aspx."/>
        /// </remarks>
        public List<string> BypassList { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to bypass local proxy
        /// </summary>
        /// <value>
        /// If set true requests to local addresses bypass the configured proxy.
        /// </value>
        public bool? BypassOnLocal { get; set; }

        /// <summary>
        /// Gets or sets proxy credentials
        /// </summary>
        public ICredentials Credentials { get; set; }

        /// <summary>
        /// Gets or sets proxy host
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Gets or sets proxy port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets proxy settings from PowerShell settings variable.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>Proxy settings if variable was present; else <c>null</c></returns>
        internal static ProxySettings GetFromSettingsVariable(SessionState session)
        {
            ProxySettings result = null;
            var variable = session.PSVariable.Get("AWSProxy");

            if (variable?.Value != null)
            {
                result = variable.Value as ProxySettings;
            }

            return result;
        }

        /// <summary>
        /// Gets the web proxy.
        /// </summary>
        /// <returns>A <see cref="WebProxy"/>, or <c>null</c> if no proxy.</returns>
        internal WebProxy GetWebProxy()
        {
            if (string.IsNullOrEmpty(this.Hostname) || this.Port <= 0)
            {
                return null;
            }

            var webProxy = new WebProxy(
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

            return webProxy;
        }
    }
}