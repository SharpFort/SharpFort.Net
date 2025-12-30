using System.Text.RegularExpressions;

var msgFile = Args[0];
var msg = File.ReadAllText(msgFile).Trim();

var pattern = @"^(feat|fix|docs|style|refactor|perf|test|chore|revert|build)(\(.*\))?!?: .+$";

if (!Regex.IsMatch(msg, pattern))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: Invalid commit message format.");
    Console.ResetColor();
    Console.WriteLine("\nExpected format: <type>: <description>");
    Console.WriteLine("Example: feat: add new login feature");
    Console.WriteLine("\nValid types: feat, fix, docs, style, refactor, perf, test, chore, revert, build");
    Console.WriteLine("\nRefer to COMMIT_CONVENTION.md for more details.");
    Environment.Exit(1);
}
