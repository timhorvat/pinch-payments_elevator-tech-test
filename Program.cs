using pinch_payments_tech_test;

var elevator = new Elevator();

Console.WriteLine("Elevator console");
Console.WriteLine("Type 'help' for commands. Type 'exit' to quit.");
Console.WriteLine();
Console.Write("> ");

while (true)
{
    var line = Console.ReadLine();
    if (line == null) break;

    var input = line.Trim();
    if (input.Length == 0)
    {
        Console.Write("> ");
        continue;
    }

    if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        var cmd = HandleCommand(elevator, input);
        if (!cmd)
        {
            Console.WriteLine("Unknown command. Type 'help' to see options.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.Write("> ");
}

bool HandleCommand(Elevator elevator, string input)
{
    var parts = SplitArgs(input);
    var cmd = parts[0].ToLowerInvariant();

    switch (cmd)
    {
        case "help":
            Console.WriteLine("Commands:");
            Console.WriteLine("  hall <floor> <up|down>   - hall call at a floor");
            Console.WriteLine("  car <floor>              - car call to a floor");
            Console.WriteLine("  test <1|2|3|4>           - run a predefined scenario");
            Console.WriteLine("  help");
            Console.WriteLine("  exit");
            return true;

        case "test":
            {
                if (parts.Length < 2 || !int.TryParse(parts[1], out var test) || test < 1 || test > 4)
                {
                    Console.WriteLine("Usage: test <1|2|3|4>");
                    return true;
                }
                RunTest(elevator, test);
                return true;
            }

        case "hall":
            {
                if (parts.Length < 3)
                {
                    Console.WriteLine("Usage: hall <floor> <up|down>");
                    return true;
                }
                if (!TryParseFloor(parts[1], out var floor))
                {
                    Console.WriteLine("Invalid floor.");
                    return true;
                }
                var dirToken = parts[2].ToLowerInvariant();


                Direction dir = dirToken switch
                {
                    "up" => Direction.UP,
                    "down" => Direction.DOWN,
                    _ => throw new ArgumentException("Direction must be 'up' or 'down'.")
                };
                elevator.HallCall(floor, dir);
                return true;
            }

        case "car":
            {
                if (parts.Length < 2)
                {
                    Console.WriteLine("Usage: car <floor>");
                    return true;
                }
                if (!TryParseFloor(parts[1], out var floor))
                {
                    Console.WriteLine("Invalid floor.");
                    return true;
                }
                elevator.CarCall(floor);
                return true;
            }
    }

    return false;
}

static bool TryParseFloor(string token, out int floor)
{
    token = token.Trim().ToLowerInvariant();

    if (int.TryParse(token, out var n))
    {
        floor = n;
        return floor >= 0 && floor <= 10;
    }

    floor = -1;
    return false;
}

static string[] SplitArgs(string input)
{
    return input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

bool WaitUntilFloorAndDirection(Elevator e, int targetFloor, Direction dir, int timeoutMs)
{
    var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
    while (DateTime.UtcNow < deadline)
    {
        // Accept IDLE at that floor too, since the car stops briefly in IDLE between actions
        if (e.CurrentFloor == targetFloor && (e.CurrentDirection == dir || e.CurrentDirection == Direction.IDLE))
            return true;
        Thread.Sleep(50);
    }
    Console.WriteLine($"Timed out waiting for floor {targetFloor} while going {dir}.");
    return false;
}

void RunTest(Elevator elevator, int test)
{
    switch (test)
    {
        // test 1: 'Passenger summons lift on the ground floor. Once in, choose to go to level 5.'
        case 1:
            elevator.HallCall(0, Direction.UP);
            WaitUntilFloorAndDirection(elevator, 0, Direction.UP, 15000);
            elevator.CarCall(5);
            break;

        // test 2: 'Passenger summons lift on level 6 to go down. Passenger on level 4 summons the lift to go down. They both choose L1.'
        case 2:
            elevator.HallCall(6, Direction.DOWN);
            elevator.HallCall(4, Direction.DOWN);
            WaitUntilFloorAndDirection(elevator, 6, Direction.DOWN, 25000);
            elevator.CarCall(1);
            WaitUntilFloorAndDirection(elevator, 4, Direction.DOWN, 25000);
            elevator.CarCall(1);
            break;

        // test 3: 'P1 summons up from L2. P2 summons down from L4. P1 -> L6. P2 -> Ground.'
        case 3:
            elevator.HallCall(2, Direction.UP);
            elevator.HallCall(4, Direction.DOWN);
            WaitUntilFloorAndDirection(elevator, 2, Direction.UP, 25000);
            elevator.CarCall(6);
            // Later, when coming down, pick up P2 then set destination
            WaitUntilFloorAndDirection(elevator, 4, Direction.DOWN, 35000);
            elevator.CarCall(0);
            break;

        // test 4: 'P1 up from Ground -> L5. P2 down from L4. P3 down from L10. P2 and P3 -> Ground.'
        case 4:
            elevator.HallCall(0, Direction.UP);
            WaitUntilFloorAndDirection(elevator, 0, Direction.UP, 15000);
            elevator.CarCall(5);

            elevator.HallCall(4, Direction.DOWN);
            elevator.HallCall(10, Direction.DOWN);

            // The car will ignore Down calls while going up, then reverse and service them
            WaitUntilFloorAndDirection(elevator, 10, Direction.DOWN, 40000);
            elevator.CarCall(0);

            WaitUntilFloorAndDirection(elevator, 4, Direction.DOWN, 30000);
            elevator.CarCall(0);
            break;
    }

    Console.WriteLine($"--- Test {test} queued. Let it finish ---");
}
