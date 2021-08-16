namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    /// <summary>
    /// Implementation of SVG renderer using <see href="https://quickchart.io/documentation/graphviz-api"/>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.ChangeVisualisation.ISvgRenderer" />
    internal class QuickChartSvgRenderer : ISvgRenderer
    {
        /// <summary>
        /// The HTTP client
        /// </summary>
        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <returns>
        /// Status of the external rendering API
        /// </returns>
        public async Task<RendererStatus> GetStatus()
        {
            try
            {
                using (var request = new HttpRequestMessage
                                         {
                                             Content = new JsonContent(new { graph = "graph{a--b}", layout = "dot", format = "svg" }),
                                             Method = HttpMethod.Post,
                                             RequestUri = new Uri("https://quickchart.io/graphviz")
                                         })
                {
                    using (var response = await HttpClient.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return RendererStatus.Ok;
                        }

                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            return RendererStatus.NotFound;
                        }

                        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                        {
                            return RendererStatus.ClientError;
                        }

                        return RendererStatus.ServerError;
                    }
                }
            }
            catch (HttpRequestException)
            {
                return RendererStatus.ConnectionError;
            }
        }

        /// <summary>
        /// Renders DOT to SVG.
        /// </summary>
        /// <param name="dotGraph">The DOT graph.</param>
        /// <returns>
        /// An <see cref="XElement" /> containing the SVG element.
        /// </returns>
        public async Task<XElement> RenderSvg(string dotGraph)
        {
            using (var request = new HttpRequestMessage
                                     {
                                         Content = new JsonContent(new { graph = dotGraph, layout = "dot", format = "svg" }),
                                         Method = HttpMethod.Post,
                                         RequestUri = new Uri("https://quickchart.io/graphviz")
                                     })
            {
                using (var response = await HttpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();

                    var doc = XDocument.Load(await response.Content.ReadAsStreamAsync());
                    return doc.Elements().FirstOrDefault();
                }
            }
        }
    }
}