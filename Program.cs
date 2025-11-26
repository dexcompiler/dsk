using ConsoleAppFramework;
using Dsk;

var app = ConsoleApp.Create();
app.Add<DskCommands>();
app.Run(args);

