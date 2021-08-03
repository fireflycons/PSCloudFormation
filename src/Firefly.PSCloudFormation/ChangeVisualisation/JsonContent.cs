namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System.Net.Http;
    using System.Text;

    using Newtonsoft.Json;

    /// <summary>
    /// A <c>JsonContent</c> class without requiring <c>System.Net.Http.Json</c>
    /// </summary>
    /// <seealso cref="System.Net.Http.StringContent" />
    internal class JsonContent : StringContent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonContent"/> class.
        /// </summary>
        /// <param name="obj">The object.</param>
        public JsonContent(object obj)
            : base(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")
        {
        }
    }
}