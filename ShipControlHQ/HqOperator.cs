// See https://aka.ms/new-console-template for more information
using ShipControlCommon;
using ShipControlHQ;

/**
 * This is a simple CLI to manage the server lifecycle, push ship commands, and inspect server status.
 */

DumpCliBanner();

while (true)
{
    DumpCliOptions();

    string choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            StartServer();
            break;
        case "2":
            DumpStatus();
            break;
        case "3":
            DumpRequests();
            break;
        case "4":
            SendCommand();
            break;
        case "5":
            StopServer();
            break;
        case "6":
            return;
        default:
            Console.WriteLine("Invalid choice");
            break;
    }
}

static void DumpCliBanner()
{
    Console.WriteLine("-==================================-");
    Console.WriteLine("-=        SHIP CONTROL HQ         =-");
    Console.WriteLine("-=          OPERATOR CLI          =-");
    Console.WriteLine("-==================================-");
}

static void DumpCliOptions()
{
    Console.WriteLine("-==================================-");
    Console.WriteLine("Select one of the following options:");
    Console.WriteLine("1. Start the server");
    Console.WriteLine("2. Show server status");
    Console.WriteLine("3. Show last requests");
    Console.WriteLine("4. Send command to a ship");
    Console.WriteLine("5. Stop the server");
    Console.WriteLine("6. Exit this CLI");
    Console.WriteLine("-==================================-");
}

static async void StartServer()
{
    await HqManager.Instance.Start("http://localhost:8080");
}

static void StopServer()
{
    HqManager.Instance.Stop();
}

static void DumpStatus()
{
    var status = HqManager.Instance.GetStatus();
    Console.WriteLine($"Server status: {(status.hqRunning ? "Running" : "Not Running")}");
    Console.WriteLine($"Connected clients ({status.connectedClientsId.Count}): {String.Join(", ", status.connectedClientsId)}");
}

static void DumpRequests()
{
    // TODO
}

static void SendCommand()
{
    Console.WriteLine("Ship ID:");
    var shipIdStr = Console.ReadLine();

    if (!HqManager.Instance.IsValidShipId(shipIdStr))
    {
        Console.WriteLine("Invalid/unknown ship");
        return;
    }

    var shipId = Guid.Parse(shipIdStr);

    Console.WriteLine("Select command to send:");
    Console.WriteLine("1. Execute process");
    Console.WriteLine("2. Read file");
    Console.WriteLine("3. Write file");
    var choice = Console.ReadLine();

    var cmd = new Command();
    switch (choice)
    {
        case "1":
            cmd.Type = Command.CmdType.ExecProcess;
            Console.WriteLine("Process name to execute:");
            cmd.Input.Add(Console.ReadLine());
            Console.WriteLine("Process arguments:");
            cmd.Input.Add(Console.ReadLine());
            break;
        case "2":
            cmd.Type = Command.CmdType.ReadFile;
            Console.WriteLine("File path to read:");
            cmd.Input.Add(Console.ReadLine());
            break;
        case "3":
            cmd.Type = Command.CmdType.WriteFile;
            Console.WriteLine("File path to write:");
            cmd.Input.Add(Console.ReadLine());
            Console.WriteLine("File content to write:");
            cmd.Input.Add(Console.ReadLine());
            break;
        default:
            Console.WriteLine("Unsupported command");
            return;
    }
    HqManager.Instance.AddCommand(shipId, cmd);
    Console.WriteLine("Command request queued to ship");
}
