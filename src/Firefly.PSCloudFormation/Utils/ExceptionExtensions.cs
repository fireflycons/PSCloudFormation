namespace Firefly.PSCloudFormation.Utils
{
    using System;
    using System.Linq;

    /// <summary>
    /// Extensions to <see cref="Exception"/> class
    /// </summary>
    internal static class ExceptionExtensions
    {
        /// <summary>
        /// Find an inner exception of the given type.
        /// </summary>
        /// <typeparam name="T">Type of exception to find</typeparam>
        /// <param name="self">Exception the method is attached to</param>
        /// <returns>Requested exception if found; else <c>null</c></returns>
        public static T FindInner<T>(this Exception self)
            where T : Exception
        {
            if (self is AggregateException aex)
            {
                return aex.InnerExceptions.Select(FindInnerRecurse<T>)
                    .FirstOrDefault(e => e != null && typeof(T) == e.GetType());
            }

            return FindInnerRecurse<T>(self.InnerException);
        }

        /// <summary>
        /// Recursively search through inner exceptions looking for requested exception.
        /// </summary>
        /// <typeparam name="T">Type of exception to find</typeparam>
        /// <param name="ex">The exception to search.</param>
        /// <returns>Requested exception if found; else <c>null</c></returns>
        private static T FindInnerRecurse<T>(Exception ex)
            where T : Exception
        {
            while (true)
            {
                if (ex == null)
                {
                    return null;
                }

                if (ex.GetType() == typeof(T))
                {
                    return (T)ex;
                }

                ex = ex.InnerException;
            }
        }
    }
}