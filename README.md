# Target Table

A WPF desktop planning app for managing daily tasks, weekly progress, work timers, and quick queue notes with SQLite-backed persistence.

---

## Features

- `To Do` column plus 7 daily columns
- Inline editable task inputs
- Per-task priority toggle with light red highlighting
- Undo for recently removed tasks with footer action and `Ctrl+Z`
- Daily work timer with play/pause, app-close persistence, and frozen past-day behavior
- Weekly stats and progress tracking
- `Work Queues` 3x4 planning grid
- JSON import and export
- SQLite persistence with auto-save behavior
- Custom desktop window chrome and streamlined UI

---

## Tech Stack

- Frontend: WPF (.NET)
- Architecture: MVVM
- Language: C#
- Database: SQLite with EF Core

---

## Project Structure

```text
TimeTableApp
|-- Assets
|   `-- app.ico
|-- Behaviors
|-- Converters
|-- Data
|   `-- TimeTableDbContext.cs
|-- Models
|   |-- AppExportData.cs
|   |-- DayTaskStatus.cs
|   |-- PersistedDayTimer.cs
|   |-- PersistedTaskItem.cs
|   `-- PersistedWorkQueueCell.cs
|-- Services
|   `-- SQLiteDataService.cs
|-- ViewModels
|   |-- BaseViewModel.cs
|   |-- DayColumnViewModel.cs
|   |-- MainViewModel.cs
|   |-- RelayCommand.cs
|   `-- WorkQueueCellViewModel.cs
|-- MainWindow.xaml
`-- MainWindow.xaml.cs
```

---

## Data Overview

- Each column is represented by `DayColumnViewModel`
- Each task row is represented by `DayTaskStatus`
- Work queue cells are represented by `WorkQueueCellViewModel`
- Persistent data includes task state, priority, timer values, timer running state, and queue cell values

Stored values include:

```text
DayIndex             -> Monday-Sunday / column index
DisplayOrder         -> Task row order
TaskName             -> Task title
Points               -> Task score value
IsDone               -> Completion state
IsPriority           -> Priority flag
ElapsedMilliseconds  -> Saved work timer value
IsRunning            -> Whether the timer was active
RunningStartedUtc    -> UTC timestamp used to continue the timer after reopen
```

---

## Persistence

- The app loads saved data from SQLite at startup
- Changes are saved automatically during normal interaction
- Running timers continue logically while the app is closed and catch up on reopen
- Data can also be exported to and imported from JSON

SQLite file location:

```text
C:\Users\<User>\AppData\Local\TimeTableApp\timetable.db
```

---

## Getting Started

### 1. Restore dependencies

```bash
dotnet restore
```

### 2. Run the app

```bash
dotnet run
```

You can also open the project in Visual Studio and start it from there.

---

## Build

```bash
dotnet build TimeTableApp.csproj
```

---

## Security Note

You may see this warning during restore or build:

```text
Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity vulnerability
```

That warning means the current SQLite native library version referenced by the project has a published advisory. The app can still build and run, but the package should be upgraded to a patched version.

Recommended follow-up:

- Update `SQLitePCLRaw.lib.e_sqlite3` directly, or
- Update the parent SQLite / EF Core package that brings it in transitively

Then verify with:

```bash
dotnet restore
dotnet build TimeTableApp.csproj
```
