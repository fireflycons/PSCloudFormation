namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System.Threading.Tasks;
    using System.Xml.Linq;

    /// <summary>
    /// Interface to SVG rendering engines
    /// </summary>
    internal interface ISvgRenderer
    {
        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <returns>Status of the external rendering API</returns>
        Task<RendererStatus> GetStatus();

        /// <summary>
        /// Renders DOT to SVG.
        /// </summary>
        /// <param name="dotGraph">The DOT graph.</param>
        /// <returns>An <see cref="XElement"/> containing the SVG element.</returns>
        Task<XElement> RenderSvg(string dotGraph);
    }
}