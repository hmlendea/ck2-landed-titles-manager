using System;

namespace CK2LandedTitlesExtractor.Exceptions
{
    /// <summary>
    /// Repository exception.
    /// </summary>
    public class DuplicateEntityException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CK2LandedTitlesExtractor.Exceptions.DuplicateEntityException"/> class.
        /// </summary>
        public DuplicateEntityException()
            : base("The specified entity already exists")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CK2LandedTitlesExtractor.Exceptions.DuplicateEntityException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        public DuplicateEntityException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CK2LandedTitlesExtractor.Exceptions.DuplicateEntityException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="innerException">Inner exception.</param>
        public DuplicateEntityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
