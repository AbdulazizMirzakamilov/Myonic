# План реализации системы целей (Goals & Targets) Myonic

> **Для агентов:** ТРЕБУЕТСЯ ПОДСКИЛЛ: Используйте `executing-plans` для реализации этого плана шаг за шагом с отметкой чекбоксов (`- [ ]`).

## 1. Обзор архитектуры (Раздел 17 ТЗ)

Система целей объединяет силовые показатели, антропометрию и календарь в единый мотивирующий центр прогресса:
- **Типы целей (`GoalType`)**:
  - `ExerciseWeight` — рабочий вес или рекорд в упражнении (например, Жим лёжа 100 → 120 кг).
  - `BodyWeight` — целевой вес тела (как снижение, так и набор, например 82 → 75 кг или 70 → 78 кг).
  - `BodyMeasurement` — целевой объем (например, Бицепс 37 → 40 см, Талия 85 → 78 см).
  - `WorkoutsFrequency` — тренировок в месяц (например, 12 → 16 тренировок в месяц).
- **Автоматическая синхронизация текущих значений (`SyncGoalCurrentValuesAsync`)**:
  - Текущие показатели берутся автоматически из базы (PR, замеров и сессий), атлету не нужно вводить их повторно.
  - При достижении целевого значения цель автоматически получает статус `Achieved` («Достигнуто! 🎉»).
- **Математика прогресса**:
  - Поддержка целей как на возрастание (жим, присед, бицепс), так и на убывание (похудение, уменьшение талии): `ProgressPercentage` (0–100%) рассчитывается точно от начального значения к целевому.
- **Интеграция в интерфейс**:
  - Экран `GoalsPage` со списком активных целей, прогресс-барами Kinetic Obsidian и фильтрами.
  - Экран создания/редактирования `GoalEditPage` с умной автоподстановкой текущих параметров.
  - Виджет целей на экране `ProfilePage` для мгновенного контроля фокуса атлета.

---

## Задачи реализации

- [ ] **Задача 1: Модели предметной области и репозиторий целей (`Myonic.Core` и `Myonic.Data`)**
  - [ ] Создать перечисления `GoalType` и `GoalStatus` в `Myonic.Core/Models/Enums.cs`.
  - [ ] Создать модель `Goal.cs` в `Myonic.Core/Models/` (Id, Title, Type, ExerciseId, ExerciseName, BodyPart, StartValue, TargetValue, CurrentValue, TargetDate, Status, Notes, CreatedAt, AchievedAt).
  - [ ] Создать record `GoalProgress` с расчетом процента выполнения, оставшейся разницы и статуса.
  - [ ] Создать интерфейс `IGoalRepository.cs` в `Myonic.Core/Repositories/`:
    - `Task<int> AddAsync(Goal goal);`
    - `Task UpdateAsync(Goal goal);`
    - `Task DeleteAsync(int id);`
    - `Task<Goal?> GetByIdAsync(int id);`
    - `Task<IReadOnlyList<Goal>> GetActiveGoalsAsync();`
    - `Task<IReadOnlyList<Goal>> GetAllGoalsAsync();`
    - `Task SyncCurrentValuesAsync();`
  - [ ] Добавить `typeof(Goal)` в `MyonicDatabase.cs` в `ModelTypes`.
  - [ ] Реализовать `GoalRepository.cs` в `Myonic.Data/Repositories/`:
    - Автоподтягивание текущего 1RM/MaxWeight из `PersonalRecord` / `WorkoutSession`.
    - Автоподтягивание текущего веса / замеров из `BodyMeasurement`.
    - Подсчет сессий за текущий календарный месяц для целей регулярности.
    - Автоматическая фиксация `Status = GoalStatus.Achieved` и `AchievedAt = DateTime.UtcNow` при достижении целевого значения.
  - [ ] Написать модульные тесты в `Myonic.Tests/GoalRepositoryTests.cs`.
  - [ ] Запустить `dotnet test Myonic.Tests\Myonic.Tests.csproj` и убедиться в успешном прохождении всех тестов.

- [ ] **Задача 2: ViewModels и экраны целей (`Myonic.App`)**
  - [ ] Создать `GoalsViewModel.cs` в `Myonic.App/ViewModels/`:
    - Загрузка активных и завершенных целей.
    - Команда создания новой цели, редактирования и удаления.
    - Форматирование строк прогресса («Осталось 4.5 кг», «Выполнено 75%»).
  - [ ] Создать `GoalsPage.xaml` и `.xaml.cs` в `Myonic.App/Pages/`:
    - Фирменные карточки целей с градиентными/акцентными прогресс-барами Kinetic Obsidian.
    - Иконки категорий (силовая, вес, замеры, частота).
    - Блок «В процессе» и блок «Достигнутые цели».
  - [ ] Создать `GoalEditViewModel.cs` в `Myonic.App/ViewModels/`:
    - Выбор типа цели (Силовая, Вес тела, Замер, Регулярность).
    - Для силовой: выбор упражнения (автоподстановка текущего рекорда как `StartValue`).
    - Для веса: автоподстановка последнего веса как `StartValue`.
    - Поля целевого значения, дедлайна и заметки.
  - [ ] Создать `GoalEditPage.xaml` и `.xaml.cs` в `Myonic.App/Pages/`.

- [ ] **Задача 3: Интеграция в профиль, регистрация в DI и навигации Shell**
  - [ ] В `ProfileViewModel.cs`:
    - Добавить загрузку активных целей (`ActiveGoalsCount`, топ-2 цели для карточки).
    - Команда `OpenGoalsCommand` для перехода на `GoalsPage`.
  - [ ] В `ProfilePage.xaml`:
    - Добавить карточку «ЦЕЛИ И НОРМАТИВЫ» с мини-прогресс-барами.
    - Добавить пункт меню в раздел быстрого доступа.
  - [ ] В `MauiProgram.cs`:
    - Зарегистрировать `IGoalRepository`, ViewModels и Pages.
  - [ ] В `AppShell.xaml.cs`:
    - Зарегистрировать маршруты `GoalsPage` и `GoalEditPage`.

- [ ] **Задача 4: Полная верификация, компиляция и пуш в GitHub**
  - [ ] Запустить unit-тесты (`dotnet test`).
  - [ ] Проверить сборку под Android (`net10.0-android`).
  - [ ] Проверить сборку под Windows (`net10.0-windows10.0.19041.0`).
  - [ ] Закоммитить и запушить все изменения в `origin/main`.
