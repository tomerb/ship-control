using ShipControlCommon;

namespace ShipControlHQ
{
    class HqRequest : Request
    {
        enum Status
        {
            Pending,
            InProgress,
            Failed,
            Completed
        }

        Status status;
        DateTime addedTimestamp;
        DateTime dispatchTimestamp;

        public HqRequest(Command cmd) : base(cmd)
        {
        }
    }
}
