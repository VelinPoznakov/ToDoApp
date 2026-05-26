namespace TodoApp.GCommon.Exceptions
{
    public class SectionNotFound: Exception
    {
        public SectionNotFound() { }

        public SectionNotFound(string? message) : base(message) { }
    }
}
