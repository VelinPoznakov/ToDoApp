namespace TodoApp.GCommon.Exceptions
{
    public class ConnectionStringNotFound: Exception
    {
        public ConnectionStringNotFound() { }

        public ConnectionStringNotFound(string message) : base(message) { }
    }
}
