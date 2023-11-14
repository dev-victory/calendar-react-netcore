using System.Data.Common;
using System.Runtime.Serialization;

namespace EventService.Application.Exceptions
{
    [Serializable]
    public class DatabaseException : DbException
    {
        public DatabaseException()
        {
        }

        public DatabaseException(string? message) : base(message)
        {
        }

        public DatabaseException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected DatabaseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}