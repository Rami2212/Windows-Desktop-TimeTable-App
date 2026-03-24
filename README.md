# Windows Desktop TimeTable App

A modern desktop timetable and task tracking application built with WPF (.NET), featuring daily task management, progress tracking, and SQLite-based persistence.

---

## Features

- Weekly layout (Monday to Sunday)
- Editable task names (like Excel)
- Assign points to each task
- Mark tasks as completed
- Automatic progress calculation per day
- Add / remove task rows dynamically
- Persistent storage using SQLite
- Auto-save on every change
- Clean Windows-style UI

---

## Tech Stack

- **Frontend:** WPF (.NET)
- **Architecture:** MVVM
- **Database:** SQLite (EF Core)
- **Language:** C#

---

## Project Structure

```
TimeTableApp
│
├── Data
│   └── TimeTableDbContext.cs
│
├── Models
│   ├── Task.cs
│   ├── DayTaskStatus.cs
│   └── PersistedTaskItem.cs
│
├── Services
│   └── SQLiteDataService.cs
│
├── ViewModels
│   ├── BaseViewModel.cs
│   ├── RelayCommand.cs
│   ├── DayColumnViewModel.cs
│   └── MainViewModel.cs
│
├── Assets
│   └── app.ico
│
└── MainWindow.xaml
```

---

## How It Works

### Data Model

- Each day is represented using `DayColumnViewModel`
- Each task row is wrapped in `DayTaskStatus`
- Persistent data stored in SQLite as:

```
DayIndex (0-6)   -> Monday-Sunday
DisplayOrder     -> Row position
TaskName         -> Task title
Points           -> Score value
IsDone           -> Completion state
```

### Persistence Flow

- App starts and loads data from SQLite
- UI updates instantly via MVVM bindings
- Any change (edit, checkbox, add/remove row) triggers:

```
DataChanged -> SaveAllDays()
```

- Data is rewritten to SQLite

---

## Database

SQLite file is auto-created at:

```
C:\Users\<User>\AppData\Local\TimeTableApp\timetable.db
```

No manual setup required.

---

## Getting Started

### 1. Clone the repository

```bash
git clone <your-repo-url>
```

### 2. Install dependencies

```bash
dotnet restore
```

### 3. Run the app

```bash
dotnet run
```

Or open in Visual Studio and press Start.

---

## Build / Publish

To create a standalone executable:

1. Right-click the project and select **Publish**
2. Choose: Folder, Self-contained, win-x64
3. Enable: Single file

Output: `TimeTableApp.exe`