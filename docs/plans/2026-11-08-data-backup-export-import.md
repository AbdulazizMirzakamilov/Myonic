# Раздел 29: Экспорт, импорт и резервное копирование данных — План реализации

> **Требование ТЗ (Раздел 29 & 31):** «Экспорт в JSON, импорт из JSON, создание резервной копии SQLite (.db3), восстановление из бэкапа, очистка данных. Критически важно для сохранности истории тренировок.»

**Цель:** Реализовать полный цикл защиты, миграции и восстановления данных атлета в Myonic: экспорт всей истории (тренировки, подходы, программы, упражнения, замеры, рекорды, цели) в переносимый JSON, импорт из JSON с валидацией целостности, создание атомарных `.db3` копий SQLite, восстановление из файла бэкапа и безопасную очистку пользовательских данных с подтверждением.

**Архитектура:**
- `Myonic.Core/Models/BackupData.cs` — DTO-структуры снимка данных (`BackupDataRoot`, `BackupMetadata`, и DTO таблиц).
- `Myonic.Core/Services/IDataBackupService.cs` — контракт сервиса бэкапа и миграции.
- `Myonic.Data/Services/DataBackupService.cs` — реализация сервиса: экспорт/импорт JSON через `System.Text.Json`, управление файлами `.db3` через `VACUUM INTO`, восстановление базы с безопасным закрытием пула соединений, каскадная очистка данных.
- `Myonic.App/ViewModels/SettingsViewModel.cs` + `SettingsPage.xaml` — интерфейс управления данными в дизайн-системе Kinetic Obsidian:
  - Карточки экспорта и импорта JSON с системным диалогом `Share` / `FilePicker`.
  - Управление локальными резервными копиями SQLite (список существующих файлов с датой/размером, кнопка создания, кнопка отправки, восстановление).
  - Опасная зона (Danger Zone): очистка всех пользовательских данных с двойным подтверждением.
- `Myonic.Tests/DataBackupServiceTests.cs` — модульные тесты экспорта, валидации, импорта и очистки данных.

---

## Задачи реализации

### Задача 1: Модели DTO и интерфейс сервиса (`Myonic.Core`)
- Создать `Myonic.Core/Models/BackupData.cs`:
  - `BackupMetadata` (SchemaVersion = 1, ExportDate, AppVersion, TotalWorkouts, TotalSets, etc.).
  - `BackupDataRoot` (Metadata, Exercises, TrainingPrograms, ProgramWorkouts, ProgramExercises, WorkoutSessions, WorkoutExercises, SetEntries, PersonalRecords, BodyMeasurements, Goals, ScheduledWorkouts).
- Создать `Myonic.Core/Services/IDataBackupService.cs`:
  - `Task<string> ExportToJsonAsync(string? targetFilePath = null)`
  - `Task<BackupSummary> ImportFromJsonAsync(string jsonContent, bool replaceExisting = true)`
  - `Task<string> CreateSqliteBackupAsync(string? targetDirectory = null)`
  - `Task<bool> RestoreSqliteBackupAsync(string backupFilePath)`
  - `Task<IReadOnlyList<BackupFileInfo>> GetAvailableBackupsAsync()`
  - `Task DeleteBackupFileAsync(string backupFilePath)`
  - `Task ClearAllUserDataAsync(bool keepDefaultExercises = true)`

### Задача 2: Реализация DataBackupService в `Myonic.Data`
- Создать `Myonic.Data/Services/DataBackupService.cs`:
  - `ExportToJsonAsync`: вычитывание всех сущностей через SQLite-соединение, сборка в `BackupDataRoot`, сериализация с `JsonSerializerOptions` (Indent = true, camelCase / PascalCase).
  - `ImportFromJsonAsync`: валидация метаданных, парсинг JSON, атомарная вставка в транзакции `RunInTransactionAsync`, сброс и перезагрузка.
  - `CreateSqliteBackupAsync`: вызов `MyonicDatabase.BackupAsync` в директорию `Backups/`, возврат пути.
  - `RestoreSqliteBackupAsync`: временное закрытие пула `_connection.CloseAsync()`, проверка валидности SQLite заголовка файла, копирование файла в рабочий путь базы `_path`, повторное открытие с прогоном миграций.
  - `GetAvailableBackupsAsync`: сканирование директории `Backups/`, сортировка по убыванию даты.
  - `ClearAllUserDataAsync`: удаление данных из таблиц `SetEntry`, `WorkoutExercise`, `WorkoutSession`, `ProgramExercise`, `ProgramWorkout`, `TrainingProgram`, `ScheduledWorkout`, `PersonalRecord`, `BodyMeasurement`, `Goal`, `Exercise` (где `IsCustom == 1`).

### Задача 3: Модульное тестирование (`Myonic.Tests`)
- Создать `Myonic.Tests/DataBackupServiceTests.cs`:
  - Тест экспорта в JSON: проверка заполненности всех массивов, дат и структуры метаданных.
  - Тест круглого цикла (Round-trip): Экспорт -> Очистка -> Импорт -> Сверка количества записей.
  - Тест валидации поврежденного JSON (некорректный JSON выбрасывает понятное исключение).
  - Тест очистки данных: удаляет только пользовательские данные, оставляет базовые упражнения.
  - Тест атомарного бэкапа SQLite.

### Задача 4: Интеграция в UI (`SettingsViewModel.cs` и `SettingsPage.xaml`)
- Зарегистрировать `IDataBackupService` -> `DataBackupService` в `MauiProgram.cs`.
- В `SettingsViewModel`:
  - Команда `ExportJsonCommand` (сохраняет JSON во временный файл и вызывает `Share.Default.RequestAsync`).
  - Команда `ImportJsonCommand` (вызывает `FilePicker.Default.PickAsync`, читает JSON, запрашивает подтверждение, вызывает импорт, показывает отчёт о количестве импортированных объектов).
  - Команда `CreateBackupCommand` (создаёт `.db3` файл бэкапа, обновляет список доступных бэкапов).
  - Команда `RestoreBackupCommand(BackupFileInfo item)` (предупреждает о перезаписи текущей базы, закрывает и восстанавливает из файла).
  - Команда `ShareBackupCommand(BackupFileInfo item)` (`Share.Default.RequestAsync`).
  - Команда `DeleteBackupCommand(BackupFileInfo item)`.
  - Команда `ClearDataCommand` (диалог с двумя подтверждениями, очистка данных, обновление статистики базы).
  - Список `ObservableCollection<BackupFileInfo> Backups`.
- В `SettingsPage.xaml`:
  - Блок «РЕЗЕРВНОЕ КОПИРОВАНИЕ И ЭКСПОРТ (JSON / SQLITE)»:
    - Кнопки «Экспорт в JSON» и «Импорт из JSON».
    - Кнопка «Создать бэкап базы (.db3)».
    - Список существующих локальных бэкапов с кнопками «Поделиться», «Восстановить», «Удалить».
  - Блок «ОПАСНАЯ ЗОНА (DANGER ZONE)»:
    - Кнопка «Очистить все данные тренировок» с акцентной красной рамкой.

### Задача 5: Полная верификация и Git
- `dotnet test Myonic.Tests` (все тесты зеленые).
- Сборка Android (`net10.0-android`) — 0 ошибок.
- Сборка Windows (`net10.0-windows10.0.19041.0`) — 0 ошибок.
- Фиксация изменений в Git и отправка на GitHub.
