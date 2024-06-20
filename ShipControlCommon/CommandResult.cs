namespace ShipControlCommon
{
    public class CommandResult
    {
        public List<string> Output { get; set; }

        public CommandResult()
        {
            this.Output = new();
        }
    }
}
