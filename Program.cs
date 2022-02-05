using MySqlConnector;

var connectionString =
    "server=127.0.0.1;user id=root;password=test;port=3306;database=connector_tests;Command Timeout=5;Pooling=true";

var dummyCommand = "select sleep(60000);";

var degreeOfParallelism = 100;


var cancelled = false;

Console.CancelKeyPress += (_, _) => cancelled = true;

while (!cancelled)
{
    var allTasks = Enumerable
        .Range(0, degreeOfParallelism)
        .AsParallel()
        .WithDegreeOfParallelism(degreeOfParallelism)
        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
        .Select(async i =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = dummyCommand;

            await command.ExecuteNonQueryAsync(cts.Token);
            await connection.CloseAsync();

            return i;
        }).ToList();

    var secondsElapsed = 0;
    var allFinished = false;

    while (secondsElapsed < 6 && !allFinished)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        secondsElapsed++;

        allFinished = allTasks.All(t => t.IsCompleted);

        Console.WriteLine($"Finished: {allFinished}. Seconds elapsed: {secondsElapsed}");

        if (secondsElapsed == 6 && allFinished)
        {
            break;
        }
    }
}

Console.WriteLine("Successfully exited");
