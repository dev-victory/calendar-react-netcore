namespace EventService.Application.Exceptions
{
    [Serializable]
    internal class InternalErrorException : Exception
    {
        public int Code { get; set; }

        public InternalErrorException(int code, string message) : base(message)
        {
            Code = code;
        }

        public InternalErrorException(int code, string message, Exception innerException) : base(message, innerException)
        {
            Code = code;
        }

        public override string ToString()
        {
            return $"Code: {Code}, Message: {Message}";
        }
    }
}