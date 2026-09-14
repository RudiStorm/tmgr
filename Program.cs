using System.CommandLine;

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
                    Modified = File.GetLastWriteTime(file),
                    IsCompleted = isCompleted
                });
            }

            var openTasks = taskLists
                .Where(x => !x.IsCompleted)
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.Modified)
                .ToList();

            const string headingColor = "\u001b[38;2;185;202;215m";
            const string resetColor = "\u001b[0m";
            const int idWidth = 15;
            Console.WriteLine($"{headingColor} {"ID",-idWidth} P   TASK                                      MODIFIED{resetColor}");
            Console.WriteLine($"{headingColor} {new string('-', idWidth)} --- ----------------------------------------- --------{resetColor}");

            for (var index = 0; index < openTasks.Count; index++)
            {
                var item = openTasks[index];
                var title = item.Title.Length > 41 ? item.Title[..38] + "..." : item.Title;
                var id = item.UniqueId;
                var idPadding = new string(' ', Math.Max(0, idWidth - id.Length));
                var clickableId = $"\u001b]8;;{item.Link}\u001b\\{id}\u001b]8;;\u001b\\";
                Console.WriteLine($" {clickableId}{idPadding} {PriorityMarker(item.Priority),-3} {title,-41} {FormatModified(item.Modified),8}");
            }

            Console.WriteLine($"\n{openTasks.Count} task{(openTasks.Count == 1 ? "" : "s")}");
        });

        return command;
    }

    static string PriorityMarker(int priority) => priority switch
    {
        >= 75 => "\u001b[38;2;255;93;93m[!]\u001b[0m",
        > 50 => "\u001b[38;2;248;189;69m[~]\u001b[0m",
        _ => "\u001b[38;2;96;165;250m[ ]\u001b[0m"
    };

    static string FormatModified(DateTime modified)
    {
        var elapsed = DateTime.Now - modified;
        if (elapsed.TotalMinutes < 1) return "now";
        if (elapsed.TotalHours < 1) return $"{Math.Max(1, (int)elapsed.TotalMinutes)}m ago";
        if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours}h ago";

        var days = Math.Max(1, (int)elapsed.TotalDays);
        return $"{days} day{(days == 1 ? "" : "s")} ago";
    }

    public class TaskItem
    {
        public string Title { get; set; } = "";
        public string UniqueId { get; set; } = "";
        public int Priority { get; set; }
        public string Link { get; set; } = "";
        public DateTime Modified { get; set; }
        public bool IsCompleted { get; set; }
    }
}
