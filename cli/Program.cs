// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
//fib bundle --output D:\folder\bundlefile.txt
//פקודת ה-fib
using System.CommandLine;
using System.CommandLine.Invocation;
//using System.CommandLine.NamingConventionBinder;
using System.Text.RegularExpressions;

////output
//var bundleOption = new Option<FileInfo>("--output", "File path and name");
//bundleCommand.AddOption(bundleOption); //נקשר אותו ל-command
//var rootCommand = new RootCommand("Root command for File Bundler CLI");
//rootCommand.InvokeAsync(args);
////פקודת ה-bundle
//var bundleCommand = new Command("bundle", "Bundle code files to a single file");
////הפונקציה שתקרה שנריץ את הפקודת bundle
//bundleCommand.SetHandler(() =>
//{
//    Console.WriteLine("bundle command");
//});
//rootCommand.AddCommand(bundleCommand);



//output
var bundleOption = new Option<FileInfo>("--output", "File path and name");
//פקודת ה-bundle
var bundleCommand = new Command("bundle", "Bundle code files to a single file");
bundleCommand.AddOption(bundleOption); //נקשר אותו ל-command

//language
var bundleLang = new Option<string[]>("--language", "List of programing languages to include in the bundle.Use 'all' to include all files.")
{ IsRequired = true , Arity = ArgumentArity.OneOrMore };//זה צריך להיות חובה
bundleCommand.AddOption(bundleLang); //נקשר אותו ל-command
//note
var bundlenote = new Option<Boolean>("--note", "add the full path of orginal file");
bundleCommand.AddOption(bundlenote); //נקשר אותו ל-command
//sotr
var bundlesort = new Option<string>("--sort", "sort the file by name or language") { IsRequired=true};
bundleCommand.AddOption(bundlesort); //נקשר אותו ל-command
//remove-empty-lines
var bundlerel = new Option<Boolean>("--remove-empty-lines", "remove empty lines from file");
bundleCommand.AddOption(bundlerel); //נקשר אותו ל-command
//author
var bundleauthor = new Option<string>("--author", "add author name to file");
bundleCommand.AddOption(bundleauthor); //נקשר אותו ל-command

// פקודה חדשה ליצירת response file
var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");




var rootCommand = new RootCommand("Root command for File Bundler CLI");
rootCommand.AddCommand(bundleCommand);

rootCommand.InvokeAsync(args);



// Handler של הפקודה create-rsp
createRspCommand.SetHandler(() =>
{
    try
    {
        // קליטת ערכים מהמשתמש
        Console.WriteLine("Enter languages to include (comma-separated, e.g., cs,js,cpp or 'all'):");
        var languages = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(languages))
        {
            Console.WriteLine("Error: Languages are required.");
            return;
        }

        Console.WriteLine("Enter the output file name or path (e.g., bundle.txt):");
        var output = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine("Error: Output file name is required.");
            return;
        }

        Console.WriteLine("Do you want to include the file path as a note? (yes/no):");
        var noteInput = Console.ReadLine()?.Trim().ToLower();
        var note = noteInput == "yes";

        Console.WriteLine("Enter sort option (name/language):");
        var sort = Console.ReadLine()?.Trim().ToLower();
        if (sort != "name" && sort != "language")
        {
            Console.WriteLine("Error: Invalid sort option. Choose 'name' or 'language'.");
            return;
        }

        Console.WriteLine("Do you want to remove empty lines? (yes/no):");
        var removeEmptyLinesInput = Console.ReadLine()?.Trim().ToLower();
        var removeEmptyLines = removeEmptyLinesInput == "yes";

        Console.WriteLine("Enter the author's name (optional):");
        var author = Console.ReadLine()?.Trim();

        // יצירת הפקודה המלאה
        var command = $"bundle --language {languages} --output \"{output}\"";
        if (note) command += " --note";
        command += $" --sort {sort}";
        if (removeEmptyLines) command += " --remove-empty-lines";
        if (!string.IsNullOrWhiteSpace(author)) command += $" --author \"{author}\"";

        // כתיבת הפקודה לקובץ תגובה
        Console.WriteLine("Enter the response file name (e.g., command.rsp):");
        var rspFileName = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(rspFileName))
        {
            Console.WriteLine("Error: Response file name is required.");
            return;
        }

        File.WriteAllText(rspFileName, command);
        Console.WriteLine($"Response file '{rspFileName}' created successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
});


// הוספת alias לאפשרויות bundle
bundleLang.AddAlias("-l");
bundleOption.AddAlias("-o");
bundlenote.AddAlias("-n");
bundlesort.AddAlias("-s");
bundlerel.AddAlias("-r");
bundleauthor.AddAlias("-a");

// הוספת הפקודה החדשה ל-RootCommand
rootCommand.AddCommand(createRspCommand);

// Handler של הפקודה
bundleCommand.SetHandler(
    async (FileInfo output, string[] language, bool note, string sort, bool removeEmptyLines, string author) =>
    {
        try
        {
            // בדיקה וסינון קבצים
            var currentDirectory = Directory.GetCurrentDirectory();
            var files = Directory.GetFiles(currentDirectory, "*.*", SearchOption.AllDirectories)
                .Where(file => language.Contains("all") || language.Contains(Path.GetExtension(file).TrimStart('.')))
                .ToList();

            if (sort == "language")
            {
                files = files.OrderBy(file => Path.GetExtension(file)).ToList();
            }
            else
            {
                files = files.OrderBy(file => Path.GetFileName(file)).ToList();
            }

            // יצירת קובץ bundle
            using var writer = new StreamWriter(output.FullName);

            // הוספת שם המחבר אם הוגדר
            if (!string.IsNullOrEmpty(author))
            {
                writer.WriteLine($"// Author: {author}");
            }

            foreach (var file in files)
            {
                if (note)
                {
                    writer.WriteLine($"// File: {file}");
                }

                var lines = File.ReadAllLines(file);

                if (removeEmptyLines)
                {
                    lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                }

                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }

                writer.WriteLine(); // שורה ריקה בין קבצים
            }

            Console.WriteLine($"Bundle created successfully at {output.FullName}");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Error: Unauthorized access. Check file permissions.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    },
    bundleOption, bundleLang, bundlenote, bundlesort, bundlerel, bundleauthor); // שמות האופציות כאן תואמות לשמות שהוגדרו




////צורת ההפעלה של הפקודה
////הפונקציה שתקרה שנריץ את הפקודת bundle
////מקבל פונקציה ו-option
//bundleCommand.SetHandler((FileInfo output,string author) =>
//{
//    try
//    {
//        //נייצר את הקובץ
//        File.Create(output.FullName);//שם הקובץ
//        Console.WriteLine("File was created");
//        StreamWriter writer = new StreamWriter(output.FullName);//יכולת לכתוב לקבצים בצורה מסוימת
//        //add name of author
//        if (author != null)
//        {
//            writer.WriteLine(author);

//        }

//        //language coopy the relevantic items

//        //sort

//        //note copy full path

//        //remove empty lines before copy

//    }
//    catch (DirectoryNotFoundException ex)
//    {
//        Console.WriteLine("Error: File path is invalid");
//    }
//}, bundleOption,bundleauthor);

