namespace Firefly.PSCloudFormation.Utils
{
    internal static class ObjectExtensions
    {
        /// <summary>
        /// Determines whether the specified object is a scalar.
        /// </summary>
        /// <param name="self">The object.</param>
        /// <returns>
        ///   <c>true</c> if the specified object is scalar; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsScalar(this object self)
        {
            switch (self)
            {
                case string _:
                case byte _:
                case short _:
                case ushort _:
                case int _:
                case uint _:
                case long _:
                case ulong _:
                case float _:
                case double _:
                case decimal _:

                    return true;

                default:

                    return false;
            }
        }
    }
}