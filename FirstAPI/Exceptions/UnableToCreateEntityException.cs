namespace FirstAPI.Exceptions
{
    public class UnableToCreateEntityException : Exception
    {
        public UnableToCreateEntityException(string message) : base(message)
        {
        }
    }
}
