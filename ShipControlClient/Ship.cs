// See https://aka.ms/new-console-template for more information
using ShipControlCommon;
using System.Text;
using System.Text.Json;

HttpClient client = new()
{
    BaseAddress = new Uri("http://localhost:8080")
};

static CommandResult HandleCommand(Command cmd)
{
    var result = new CommandResult();
    switch (cmd.Type)
    {
        case Command.CmdType.ExecProcess:
            Console.WriteLine($"Executing process '{cmd.Input[0]}' with params: '{cmd.Input[1]}'");
            result.Output.Add("0"); // Exit code
            result.Output.Add("stdout");
            result.Output.Add("stderr");
            break;
        case Command.CmdType.ReadFile:
            Console.WriteLine($"Reading file '{cmd.Input[0]}'");
            result.Output.Add("file content");
            break;
        case Command.CmdType.WriteFile:
            Console.WriteLine($"Writing file '{cmd.Input[0]}' with content: '{cmd.Input[1]}'");
            result.Output.Add("success");
            break;
        default:
            Console.WriteLine("Unsupported command");
            break;
    }
    return result;
}

var clientIdString = Guid.NewGuid().ToString();

HttpResponseMessage response =
    await client.PostAsync(client.BaseAddress, new StringContent(clientIdString, Encoding.UTF8, "text/plain"));

var responseText = await response.Content.ReadAsStringAsync();
Console.WriteLine($"{responseText}\n");

using (var ws = new WebSocketSharp.WebSocket($"ws://localhost:9000/{clientIdString}"))
{
    ws.OnMessage += (sender, e) =>
    {
        Console.WriteLine($"Received from server: {e.Data}");
        var request = JsonSerializer.Deserialize<Request>(e.Data);
        var response = new Response(request.Id, DateTime.Now);
        response.Result = HandleCommand(request.Input);
        response.DispatchTimestamp = DateTime.Now;
        ws.Send(JsonSerializer.Serialize(response));
    };

    ws.Connect();
    Console.WriteLine("Connected to server");

    while (true)
    {
        string message = Console.ReadLine();
        if (message == "exit")
            break;

        ws.Send(message);
    }

    ws.Close();
}

Console.ReadLine();
