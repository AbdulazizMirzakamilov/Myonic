# План реализации блока антропометрии (замеров тела и динамики веса) Myonic

> **Для агентов:** ТРЕБУЕТСЯ ПОДСКИЛЛ: Используйте `executing-plans` для реализации этого плана шаг за шагом с отметкой чекбоксов (`- [ ]`).

## 1. Обзор архитектуры

Блок антропометрии позволяет пользователю отслеживать изменения композиции тела параллельно с силовыми тренировками:
- **Модель данных `BodyMeasurement`**: фиксация веса тела, процента жира, сантиметровых замеров (шея, грудь, талия, бёдра, бицепс, бедро, икры), даты и заметок.
- **Репозиторий `IBodyMeasurementRepository` / `BodyMeasurementRepository`**: атомарные операции в SQLite, получение истории, расчет тренда веса (текущий, начальный, дельта $\pm$, мин, макс).
- **Нативный векторный график веса (`WeightTrendDrawable`)**: реализация `IDrawable` на базе встроенного `Microsoft.Maui.Graphics` (без тяжелых внешних библиотек) в фирменной дизайн-системе *Kinetic Obsidian* (неоново-вольтовая линия, градиентное свечение под графиком, точки замеров, горизонтальные направляющие).
- **Экран «Замеры тела и вес» (`BodyMeasurementsPage`)**: карточка аналитики динамики, встроенный график с фильтром по времени, интерактивная история замеров с расчетом дельты к предыдущему замеру.
- **Экран добавления/редактирования замера (`BodyMeasurementEditPage`)**: эргономичный ввод веса с кнопками быстрой корректировки ($\pm 0.5$ кг), процент жира, замеры объёмов и заметки.
- **Интеграция в профиль (`ProfilePage`)**: карточка быстрого внесения веса прямо с экрана профиля и пункт меню перехода в антропометрию.

---

## Задачи реализации

- [x] **Задача 1: Модель данных и репозиторий (`Myonic.Core` и `Myonic.Data`)**
  - [x] Создать модель `BodyMeasurement.cs` в `Myonic.Core/Models/` (Id, Date, WeightKg, BodyFatPercentage, ChestCm, WaistCm, HipsCm, BicepsCm, ThighCm, CalfCm, NeckCm, Notes, CreatedAt).
  - [x] Создать record `WeightTrendStats` в `Myonic.Core/Models/` (CurrentWeight, StartWeight, DeltaKg, MinWeight, MaxWeight, MeasurementsCount).
  - [x] Создать интерфейс `IBodyMeasurementRepository.cs` в `Myonic.Core/Repositories/`:
    - `Task<int> AddAsync(BodyMeasurement measurement);`
    - `Task UpdateAsync(BodyMeasurement measurement);`
    - `Task DeleteAsync(int id);`
    - `Task<BodyMeasurement?> GetByIdAsync(int id);`
    - `Task<BodyMeasurement?> GetLatestAsync();`
    - `Task<BodyMeasurement?> GetByDateAsync(DateTime date);`
    - `Task<List<BodyMeasurement>> GetAllAsync(int limit = 50);`
    - `Task<List<BodyMeasurement>> GetWeightHistoryAsync(DateTime? fromDate = null, int limit = 60);`
    - `Task<WeightTrendStats> GetWeightTrendStatsAsync(int days = 30);`
  - [x] Зарегистрировать `typeof(BodyMeasurement)` в `MyonicDatabase.cs` в списке `ModelTypes`.
  - [x] Реализовать `BodyMeasurementRepository.cs` в `Myonic.Data/Repositories/`.
  - [x] Написать модульные тесты в `Myonic.Tests/BodyMeasurementRepositoryTests.cs`.
  - [x] Запустить `dotnet test Myonic.Tests\Myonic.Tests.csproj` и убедиться в успешном прохождении (100% тестов).

- [x] **Задача 2: Графический компонент динамики веса (`WeightTrendDrawable`)**
  - [x] Создать класс `WeightTrendDrawable.cs` в `Myonic.App/Drawables/`:
    - Поддержка списка точек `(DateTime Date, double Weight)`.
    - Отрисовка сглаженной ломаной линии графика цветом `#C4F135` (`KineticVolt`).
    - Отрисовка мягкого полупрозрачного градиента под линией до нижней границы.
    - Отрисовка маркеров точек (акцентные точки с белым ядром).
    - Отрисовка горизонтальных пунктирных направляющих (мин. и макс. значения с подписями веса).
    - Обработка граничных состояний: 0 точек (заглушка с подсказкой), 1 точка (реперная линия).

- [x] **Задача 3: Экран истории и графиков замеров (`BodyMeasurementsPage`)**
  - [x] Создать `BodyMeasurementsViewModel.cs` в `Myonic.App/ViewModels/`:
    - Загрузка замеров, расчет аналитики за 30/90/180 дней / всё время.
    - Команды добавления, выбора фильтра периода, редактирования и удаления с подтверждением.
    - Форматирование дельты к предыдущему замеру (+0.8 кг / -1.2 кг с подсветкой цветом).
  - [x] Создать `BodyMeasurementsPage.xaml` и `.xaml.cs` в `Myonic.App/Pages/`:
    - Шапка с кнопкой «+ Новый замер».
    - Карточка тренда веса (текущий вес, дельта за период, макс/мин).
    - `GraphicsView` с привязанным `WeightTrendDrawable`.
    - Переключатель периодов (1 мес, 3 мес, 6 мес, Всё время).
    - Список истории замеров `CollectionView` с карточками замеров и кнопками действий.

- [x] **Задача 4: Экран ввода/редактирования замера (`BodyMeasurementEditPage`)**
  - [x] Создать `BodyMeasurementEditViewModel.cs` в `Myonic.App/ViewModels/`:
    - Поддержка параметров навигации через `[QueryProperty]` (id замера при редактировании).
    - Поля: дата, вес, процент жира, замеры (талия, грудь, бицепс, бедра, бедро, икра, шея), заметка.
    - Быстрые команды инкремента/декремента веса ($\pm 0.5$ кг).
    - Валидация и сохранение/удаление.
  - [x] Создать `BodyMeasurementEditPage.xaml` и `.xaml.cs` в `Myonic.App/Pages/`.

- [x] **Задача 5: Интеграция в профиль и регистрация в DI и Shell**
  - [x] В `ProfileViewModel.cs`:
    - Добавить загрузку последнего замера веса (`LatestWeightText`, `WeightDeltaText`, `HasWeightData`).
    - Команда `QuickAddWeightCommand` для быстрого внесения сегодняшнего веса.
    - Команда `OpenBodyMeasurementsCommand` для перехода на `BodyMeasurementsPage`.
  - [x] В `ProfilePage.xaml`:
    - Добавить стильную карточку «ТЕКУЩИЙ ВЕС» с кнопкой «+ Внести вес».
    - Добавить пункт меню «📏 Антропометрия и замеры» в блоке разделов.
  - [x] В `MauiProgram.cs`:
    - Зарегистрировать `IBodyMeasurementRepository`, ViewModels и Pages.
  - [x] В `AppShell.xaml.cs`:
    - Зарегистрировать маршруты `BodyMeasurementsPage` и `BodyMeasurementEditPage`.

- [x] **Задача 6: Полная верификация, сборка и отправка в Git**
  - [x] Запустить все unit-тесты (`dotnet test`).
  - [x] Собрать проект под Android (`net10.0-android`) без ошибок.
  - [x] Собрать проект под Windows (`net10.0-windows10.0.19041.0`) без ошибок.
  - [x] Закоммитить и запушить все изменения в `origin/main`.
