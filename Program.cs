using System.CommandLine;
using System.Runtime.InteropServices;
using System.Text.Json;

class Program
{
    static string MainFolder { get; } = Path.Combine(Directory.GetCurrentDirectory(), "tmgr_tasks");
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("A simple tmgr command-line tool.");

        rootCommand.Add(CreateInitCommand());
        rootCommand.Add(CreateNewCommand());
        rootCommand.Add(GetTasksCommand());

        return await rootCommand.Parse(args).InvokeAsync();
    }

    static Command CreateInitCommand()
    {
        var command = new Command("init", "Initialize tmgr in the current directory.");

        command.SetAction(_ =>
        {
            if (Directory.Exists(MainFolder))
            {
                Console.WriteLine("tmgr is already initialized in this directory.");
                return;
            }

            Directory.CreateDirectory(MainFolder);

            Console.WriteLine($"Initialized tmgr in {MainFolder}");
        });

        return command;
    }

    static Command CreateNewCommand()
    {
        var command = new Command("new", "Create a new task.");

        var titleArgument = new Argument<string>("title")
        {
            Description = "The task title"
        };

        var descriptionArgument = new Argument<string>("description")
        {
            Description = "Description of task"
        };

        command.Add(titleArgument);
        command.Add(descriptionArgument);

        command.SetAction(parseResult =>
        {
            var title = parseResult.GetValue(titleArgument)!;
            var description = parseResult.GetValue(descriptionArgument) ?? "";
            CreateTask(title, description);
        });

        return command;
    }

    static void CreateTask(string title, string description)
    {
        var folder = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var folderPath = Path.Combine(MainFolder, folder);
        Directory.CreateDirectory(folderPath);

        var taskFile = Path.Combine(folderPath, "task.md");

        var content = $"""
            # {title}
            ------------------------------
            - Priority: 50
            - Completed: false
            - Tags: [tag1,tag2,tag3]
            ------------------------------
            {description ?? ""}
            """;

        File.WriteAllText(taskFile, content);

        Console.WriteLine($"New Task Created: {title} - {taskFile}");
    }

    static Command GetTasksCommand()
    {
        var command = new Command("tasks", "Get all tasks");

        command.SetAction(_ =>
        {
            var files = Directory.EnumerateFiles(MainFolder, "task.md", SearchOption.AllDirectories);
            if (!files.Any())
            {
                Console.WriteLine("No current tasks");
                return;
            }

            var taskLists = new List<TaskItem>();
            foreach (var file in files)
            {
                var uri = new Uri(file).AbsoluteUri;
                var uniqueId = Directory.GetParent(file)?.Name ?? "";
                var lines = File.ReadLines(file).Take(5).ToList();

                var title = lines.FirstOrDefault()?.Substring(2) ?? "";
                var priorityText = lines.ElementAtOrDefault(2)?.Split(':', 2).ElementAtOrDefault(1)?.Trim();
                var priority = int.TryParse(priorityText, out var priorityValue) ? priorityValue : 0;

                var completedText = lines.ElementAtOrDefault(3)?.Split(':', 2).ElementAtOrDefault(1)?.Trim();
                var isCompleted = bool.TryParse(completedText, out var completedValue) && completedValue;

                taskLists.Add(new TaskItem
                {
                    Title = title,
                    UniqueId = uniqueId,
                    Priority = priority,
                    Link = uri,
                    IsCompleted = isCompleted
                });
            }

            foreach (var item in taskLists.Where(x => !x.IsCompleted).OrderByDescending(x => x.Priority))
            {
                Console.Write($"\u001b]8;;{item.Link}\u001b\\{item.UniqueId}\u001b]8;;\u001b\\  - {item.Title} - Priority: {item.Priority}\n");
            }
        });

        return command;
    }

    public class TaskItem
    {
        public string Title { get; set; } = "";
        public string UniqueId { get; set; } = "";
        public int Priority { get; set; }
        public string Link { get; set; } = "";
        public bool IsCompleted { get; set; }
    }
}
