namespace Firefly.PSCloudFormation.Utils
{
    using System;

    /// <summary>
    /// Dumps an entire exception tree
    /// </summary>
    internal class ExceptionDumper
    {
        /// <summary>
        /// Whether to include stack traces
        /// </summary>
        private readonly bool includeStackTrace;

        /// <summary>
        /// Where to write output
        /// </summary>
        private readonly Action<string> output;

        /// <summary>
        /// The indentation level
        /// </summary>
        private int indent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionDumper"/> class.
        /// </summary>
        /// <param name="output">Where to write output.</param>
        /// <param name="includeStackTrace">if set to <c>true</c> include stack traces.</param>
        public ExceptionDumper(Action<string> output, bool includeStackTrace)
        {
            this.includeStackTrace = includeStackTrace;
            this.output = output;
        }

        /// <summary>
        /// Dumps the specified exception.
        /// </summary>
        /// <param name="ex">The exception.</param>
        public void Dump(Exception ex)
        {
            this.output("Exception Dump:");
            this.indent = -1;
            this.DumpRecursive(ex);
        }

        /// <summary>
        /// Recursively dumps the specified exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        private void DumpRecursive(Exception ex)
        {
            ++this.indent;
            if (ex != null)
            {
                this.DumpThisException(ex);

                if (ex is AggregateException aex)
                {
                    foreach (var innerException in aex.InnerExceptions)
                    {
                        this.DumpRecursive(innerException);
                    }
                }
                else
                {
                    this.DumpRecursive(ex.InnerException);
                }
            }

            --this.indent;
        }

        /// <summary>
        /// Dumps the details of an exception
        /// </summary>
        /// <param name="ex">The exception.</param>
        private void DumpThisException(Exception ex)
        {
            var padding = new string(' ', this.indent * 2);
            this.output($"- {padding}{ex.GetType().Name}: {ex.Message}");

            if (this.includeStackTrace)
            {
                foreach (var line in ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    this.output($"{padding}{line}");
                }
            }
        }
    }
}