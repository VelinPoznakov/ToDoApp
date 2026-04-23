namespace TodoApp.GCommon.Exceptions
{
    public class DataPersistFail: Exception
    {

        public DataPersistFail() { }

        public DataPersistFail(string message) : base(message) { }
    }
}
