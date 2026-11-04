# План переработки календаря тренировок Myonic

> **Для агентов:** ТРЕБУЕТСЯ ПОДСКИЛЛ: Используйте `executing-plans` для реализации этого плана шаг за шагом с отметкой чекбоксов (`- [ ]`).

## 1. Обзор архитектуры

Календарь становится единым центром расписания и истории тренировок приложения Myonic:
- **Компактный календарь** на основном экране `SchedulePage`: недельная полоса, быстрое переключение, карточки событий дня, кнопка «Открыть полный календарь».
- **Полноценный календарь** на отдельном экране `CalendarFullPage`: переключатель режимов «Месяц / Неделя», полная сетка месяца с индикаторами, сохранение выбранной даты при переключении, свайпы на Android, адаптация под Windows.
- **Единый сервис календаря `ICalendarService`**: общая бизнес-логика получения событий, статусов (Запланирована – синий, Выполнена – зеленый, Пропущена – красный) без дублирования кода.
- **Перенос истории**: вкладка «Анализ» убирается из навигации Shell и профиля; клик по выполненной тренировке в календаре открывает `WorkoutSessionDetailPage` со всеми деталями и разбором план-факт.
- **Типографика**: замена `—` на `–` во всех пользовательских текстах.

---

## Задачи реализации

- [ ] **Задача 1: Модели предметной области и сервис календаря (`Myonic.Core` и `Myonic.Data`)**
  - [ ] Создать перечисление `CalendarWorkoutStatus` (`Planned`, `Completed`, `Missed`).
  - [ ] Создать модель `CalendarWorkoutItem` (Id, Title, ProgramName, TimeText, DurationText, Status, SummaryText, списки упражнений, флаги `CanStart`, `CanViewDetails`).
  - [ ] Создать модель `CalendarDayIndicators` (HasCompleted, HasPlanned, HasMissed).
  - [ ] Создать интерфейс `ICalendarService` с методами:
    - `Task<IReadOnlyList<CalendarWorkoutItem>> GetWorkoutsForDateAsync(DateTime date);`
    - `Task<IReadOnlyDictionary<DateTime, CalendarDayIndicators>> GetIndicatorsForRangeAsync(DateTime fromDate, DateTime toDate);`
  - [ ] Реализовать `CalendarService` в `Myonic.Data.Services`:
    - Корректная загрузка завершенных сессий из `IWorkoutSessionRepository`.
    - Корректная загрузка плановых тренировок из `IWorkoutScheduleRepository`.
    - Динамическое определение `Missed` (`date.Date < DateTime.Today` и нет завершенной тренировки).
    - Исключение дубликатов: если плановая тренировка завершена в ту же дату, отображается только одна выполненная карточка со статусом `Completed`.
    - Учет переносов (`OriginalDate`) и отмен (`Status == Cancelled`), исключающий ложные пропуски на прежней дате.
  - [ ] Написать модульные тесты для `CalendarService` в `Myonic.Tests\CalendarServiceTests.cs`.
  - [ ] Запустить `dotnet test Myonic.Tests\Myonic.Tests.csproj` и убедиться в прохождении.

- [ ] **Задача 2: Доработка `ScheduleViewModel.cs`**
  - [ ] Интегрировать `ICalendarService`.
  - [ ] Поддержать два режима отображения для полного календаря: `IsMonthView` (по умолчанию `true`) и `IsWeekView` (`false`).
  - [ ] Реализовать сохранение `SelectedDate` при переключении между «Месяц» и «Неделя».
  - [ ] Сформировать `MonthDays` (35–42 дня: дни месяца + приглушенные дни смежных месяцев с `IsCurrentMonth = false`).
  - [ ] Сформировать `WeekDays` (7 дней текущей недели: ПН..ВС).
  - [ ] Сформировать единую коллекцию `Workouts` (`ObservableCollection<CalendarWorkoutItem>`) для выбранного дня.
  - [ ] Добавить команды навигации:
    - `PreviousPeriodCommand` / `NextPeriodCommand` (в режиме месяца листает месяц, в режиме недели – неделю).
    - `PreviousWeekCommand` / `NextWeekCommand` (для компактного календаря и недельного режима).
    - `PreviousMonthCommand` / `NextMonthCommand` (для месячного режима).
    - `TodayCommand` (переход на `DateTime.Today`).
    - `SelectDayCommand(CalendarDayItem day)` (выбор дня с обновлением `Workouts`).
    - `SetMonthViewCommand` / `SetWeekViewCommand`.
    - `OpenFullCalendarCommand` (навигация на `CalendarFullPage`).
    - `OpenWorkoutDetailCommand` (навигация на `WorkoutSessionDetailPage` для выполненных).
    - `StartPlannedWorkoutCommand` / `StartFreeWorkoutCommand`.
  - [ ] Заменить `—` на `–` в строках `ScheduleViewModel.cs`.
  - [ ] Зарегистрировать `ScheduleViewModel` как `AddSingleton` в `MauiProgram.cs` для общего состояния между компактным и полным экраном.

- [ ] **Задача 3: Экран полноценного календаря (`CalendarFullPage.xaml` и `.cs`)**
  - [ ] Создать `CalendarFullPage.xaml` с поддержкой Android и Windows (адаптивная разметка).
  - [ ] Реализовать переключатель режимов «Месяц / Неделя» с контрастным стилем Kinetic Obsidian.
  - [ ] В месячном режиме:
    - Заголовок с днями недели (ПН, ВТ, СР, ЧТ, ПТ, СБ, ВС).
    - Сетка месяца (7 колонок, дни месяца, приглушенные дни предыдущего/следующего месяца).
    - Индикаторы тренировок: зеленый кружок (выполнена), синий кружок (запланирована), красный кружок (пропущена).
    - Выделение сегодняшней даты и выбранного дня.
  - [ ] В недельном режиме:
    - 7 дней недели в просторном представлении с индикаторами.
  - [ ] В нижней части (или правой колонке на Desktop):
    - Список тренировок выбранного дня (`Workouts`) с бейджами статуса («Выполнена», «Запланирована», «Пропущена»).
    - Кнопки перехода к деталям или старту тренировки.
  - [ ] Жесты свайпа на Android через `PanGestureRecognizer` (влево/вправо с защитой от вертикального скролла).
  - [ ] Кнопки навигации `<` и `>` для мыши/клавиатуры на Windows.
  - [ ] Зарегистрировать `CalendarFullPage` в Shell и DI.

- [ ] **Задача 4: Компактный календарь на основном экране (`SchedulePage.xaml`)**
  - [ ] Сохранить активную плашку тренировки (`ActiveWorkoutTracker.HasActiveSession`).
  - [ ] Верхняя панель: месяц и год, кнопки `< Сегодня >`, заметная кнопка «Открыть полный календарь».
  - [ ] Горизонтальная недельная полоса из 7 дней с индикаторами (зеленый, синий, красный).
  - [ ] Жест свайпа недели на Android (`PanGestureRecognizer`).
  - [ ] Список тренировок выбранного дня с цветными бейджами статуса и действиями.
  - [ ] Карточка дня отдыха («Отдых или свободная тренировка» + «+ Начать свободную тренировку»).
  - [ ] Обновление данных в `OnAppearing` (включая автоматическое отражение только что завершенной активной тренировки).

- [ ] **Задача 5: Перенос истории тренировок и чистка навигации Shell**
  - [ ] Удалить вкладку «Анализ» (`WorkoutHistoryPage`) из `TabBar` в `AppShell.xaml`.
  - [ ] Удалить пункт «Анализ / История» из бокового меню Flyout в `AppShell.xaml`.
  - [ ] Удалить карточку «История всех тренировок» из `ProfilePage.xaml` и метод из `ProfileViewModel.cs`.
  - [ ] Обновить обработчики навигации в `AppShell.xaml.cs`.
  - [ ] Убедиться, что `WorkoutSessionDetailPage` открывается из карточек календаря с полной историей и разбором план-факт.

- [ ] **Задача 6: Проверка и исправление типографики**
  - [ ] Заменить длинное тире `—` на среднее тире `–` в пользовательских текстах:
    - `ProgramExerciseEditPage.xaml`
    - `SettingsPage.xaml`
    - `ProfileViewModel.cs`
    - `ScheduleViewModel.cs`

- [ ] **Задача 7: Тестирование и проверка сборок**
  - [ ] Запустить `dotnet test Myonic.Tests/Myonic.Tests.csproj` (все тесты должны пройти на 100%).
  - [ ] Запустить сборку Android: `dotnet build Myonic.App/Myonic.App.csproj -f net10.0-android`.
  - [ ] Запустить сборку Windows: `dotnet build Myonic.App/Myonic.App.csproj -f net10.0-windows10.0.19041.0`.

- [ ] **Задача 8: Git коммит и завершение**
  - [ ] Проверить `git status` и `git diff`.
  - [ ] Создать коммит с сообщением: `feat: enhance workout calendar experience`.
  - [ ] Запушить изменения на GitHub.
