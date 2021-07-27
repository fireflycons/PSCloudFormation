namespace Firefly.PSCloudFormation.Utils
{
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// Extension methods for <see cref="XmlDocument"/>
    /// </summary>
    internal static class XmlDocumentExtensions
    {
        /// <summary>
        /// Converts <see cref="XmlDocument"/> to <see cref="XDocument"/>
        /// </summary>
        /// <param name="xmlDocument">The XML document.</param>
        /// <returns>Converted <see cref="XDocument"/></returns>
        public static XDocument ToXDocument(this XmlDocument xmlDocument)
        {
            using (var nodeReader = new XmlNodeReader(xmlDocument))
            {
                nodeReader.MoveToContent();
                return XDocument.Load(nodeReader);
            }
        }
    }
}