namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    /// <summary>
    /// Status of SVG renderer API
    /// </summary>
    internal enum RendererStatus
    {
        /// <summary>
        /// Renderer is OK
        /// </summary>
        Ok,

        /// <summary>
        /// Renderer was not found
        /// </summary>
        NotFound,

        /// <summary>
        /// Other client error (4xx)
        /// </summary>
        ClientError,

        /// <summary>
        /// Server error (5xx)
        /// </summary>
        ServerError,

        /// <summary>
        /// Could not connect to service
        /// </summary>
        ConnectionError
    }
}