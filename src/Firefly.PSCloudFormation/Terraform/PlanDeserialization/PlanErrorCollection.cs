namespace Firefly.PSCloudFormation.Terraform.PlanDeserialization
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Collection class containing all the errors from the last <c>terraform plan</c> run.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEnumerable&lt;Firefly.PSCloudFormation.Terraform.PlanDeserialization.PlanError&gt;" />
    internal class PlanErrorCollection : IEnumerable<PlanError>
    {
        /// <summary>
        /// The errors
        /// </summary>
        private readonly List<PlanError> errors;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanErrorCollection"/> class.
        /// </summary>
        /// <param name="errors">The errors.</param>
        public PlanErrorCollection(IEnumerable<PlanError> errors)
        {
            this.errors = errors.ToList();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<PlanError> GetEnumerator()
        {
            return this.errors.GetEnumerator();
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.errors.Aggregate(0, (current, e) => current ^ e.Diagnostic.Snippet.GetHashCode());
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}