namespace ShipControlCommon
{
    public class Request
    {
        public Guid Id { get; set; }
        public Command Input { get; set; }

        public Request()
        {
            Id = Guid.NewGuid();
        }

        public Request(Command cmd)
        {
            Id = Guid.NewGuid();
            Input = cmd;
        }
    }
}
