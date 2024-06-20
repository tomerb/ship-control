namespace ShipControlCommon
{
    public struct Command
    {
        public enum CmdType
        {
            ExecProcess,
            ReadFile,
            WriteFile
        }

        public CmdType Type { set; get; }

        public List<string?> Input { get; set; }

        public Command()
        {
            this.Input = new();
        }

    }
}
