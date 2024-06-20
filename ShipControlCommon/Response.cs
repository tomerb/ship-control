namespace ShipControlCommon
{
    public class Response
    {
        public Guid Id { get; set; }
        public CommandResult? Result { get; set; }
        public DateTime ArrivalTimestamp { get; set; }
        public DateTime DispatchTimestamp { get; set; }

        public Response()
        {
        }

        public Response(Guid id, DateTime arrivalTimestamp)
        {
            Id = id;
            ArrivalTimestamp = arrivalTimestamp;
        }
    }
}
