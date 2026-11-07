# C# Assignment – Employee Time Report

A single-file C# program that reads time entries from a REST API and produces:

1. **`EmployeeReport.html`** – a table of employees ordered by total time worked (highest first). Rows for employees with less than 100 hours are highlighted in red.
2. **`EmployeePieChart.png`** – a pie chart showing each employee's percentage of the total time worked.

## File
`YourName_CSharp_Assignment.cs`

## Requirements
- Windows (the pie chart uses `System.Drawing`)
- .NET 8 SDK or newer
- NuGet package: `System.Drawing.Common`
- Internet connection (data is fetched from the API)

## How to run (Visual Studio)
1. Create a new **Console App (C#)** project.
2. Install the package: `Install-Package System.Drawing.Common`
3. Replace the contents of `Program.cs` with the code from `YourName_CSharp_Assignment.cs`.
4. Press **F5** or **Ctrl+F5**.
5. Open `bin\Debug\net8.0\` to find `EmployeeReport.html` and `EmployeePieChart.png`.

## How to run (command line)
```
dotnet new console
dotnet add package System.Drawing.Common
```
Replace `Program.cs` with the code, then run:
```
dotnet run
```

## Data handling
The program ignores entries that are:
- marked as deleted (`DeletedOn` is set)
- missing an employee name
- invalid (end time earlier than start time)

Total time is calculated per employee as the sum of `EndTimeUtc - StarTimeUtc` across the remaining entries.
