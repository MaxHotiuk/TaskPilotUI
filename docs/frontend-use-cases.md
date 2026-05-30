# TaskPilot UI — Frontend Use Cases

> Документ описує **виключно UI-взаємодії**: що користувач бачить, куди натискає і як змінюється екран. Технічні деталі реалізації наводяться лише у полі «Виклик API».
>
> **Актори:**
> - **User** — будь-який аутентифікований користувач.
> - **Board Owner** — власник дошки (`OwnerId == currentUser.Id`).
> - **Board Member** — учасник дошки з роллю Member або вище.

---

## Секція 1 — UI: Взаємодія з дошками та завданнями

---

### 1.1 Навігація до списку дошок

- **Актор(и):** User
- **Опис:** Користувач відкриває головну сторінку застосунку або переходить за URL `/boards`, щоб побачити список своїх дошок.
- **Локація в коді:** `Pages/Boards/Boards.razor`
- **Взаємодія користувача (Покроково):**
  1. Браузер завантажує сторінку `/` або `/boards`.
  2. Відображається компонент спінера (`Spin`) з текстом «Loading your boards…» поки дані завантажуються.
  3. Після завантаження відображається сітка карток дошок (`BoardSearchCard`) у адаптивному grid-layout (Ant Design `Row/Col`).
  4. У верхній частині сторінки знаходиться `OrganizationSelector` — випадаючий список для фільтрації дошок за організацією.
  5. Якщо дошок немає — відображається порожній стан із пропозицією створити першу дошку.
- **Виклик API:** `IBoardService.SearchBoardsAsync(searchTerm, filterType, organizationId, page, pageSize)` → `IBoardApi`

---

### 1.2 Пошук та фільтрація дошок

- **Актор(и):** User
- **Опис:** Користувач вводить текст у поле пошуку або обирає фільтр (All / Archived) для звуження списку дошок.
- **Локація в коді:** `Pages/Boards/Components/BoardsPageHeader.razor`, `Pages/Boards/Boards.razor.cs`
- **Взаємодія користувача (Покроково):**
  1. У компоненті `BoardsPageHeader` є поле введення (Input) та перемикачі фільтрів.
  2. Користувач вводить текст — через debounce-таймер (~300 мс) автоматично запускається пошук.
  3. Список карток перебудовується; якщо є ще сторінки — з'являється кнопка «Load more boards».
  4. При натисканні «Load more» підвантажується наступна сторінка і нові картки додаються до поточного списку.
  5. Перемикач фільтра «Archived» перезапускає запит з параметром `filterType = "archived"`.
- **Виклик API:** `IBoardService.SearchBoardsAsync(...)` з оновленими параметрами пошуку/фільтра

---

### 1.3 Перехід на сторінку дошки

- **Актор(и):** User, Board Member, Board Owner
- **Опис:** Користувач натискає на картку дошки в списку і потрапляє на сторінку деталей дошки з колонками станів.
- **Локація в коді:** `Pages/Boards/Components/BoardSearchCard.razor`, `Pages/Board/BoardDetail.razor`
- **Взаємодія користувача (Покроково):**
  1. Кожна картка `BoardSearchCard` є клікабельною.
  2. При кліку спрацьовує `OnBoardClick` → `NavigationManager.NavigateTo($"/board/{boardId}")`.
  3. Завантажується `BoardDetail.razor` з хедером (`BoardHeader`) та колонками стовпців (`BoardColumns`).
  4. Поки дані завантажуються — показується спінер `Spin` з текстом «Loading board…».
  5. Після завантаження колонки відображають картки завдань, згруповані за станами.
- **Виклик API:** `IBoardService.GetBoardDetailAsync(boardId)` → `IBoardApi`

---

### 1.4 Перегляд завдань у колонках (Board View)

- **Актор(и):** Board Member, Board Owner
- **Опис:** На сторінці дошки користувач бачить усі стани у вигляді колонок і завдання у вигляді карток всередині відповідних колонок.
- **Локація в коді:** `Pages/Board/Components/BoardColumns.razor`
- **Взаємодія користувача (Покроково):**
  1. Кожна колонка має заголовок із назвою стану і `Badge` з кількістю завдань.
  2. Картка завдання (`Card`) відображає: назву, скорочений опис, assignee (Tag з ім'ям), due date (кольоровий Tag із іконкою годинника), пріоритет (Tag із кольором), тег (кольоровий Tag).
  3. Колонки прокручуються вертикально (max-height: 600px, overflow: auto) незалежно одна від одної.
  4. Кнопка фільтра «My Tasks / All Tasks» у `BoardHeader` перемикає відображення лише завдань поточного користувача.
  5. При порожній колонці відображається текст «No tasks in this state».
- **Виклик API:** Дані беруться з `BoardDetailDto` завантаженого на кроці 1.3; фільтр «Only Mine» не викликає окремий API — фільтрація виконується на клієнті.

---

### 1.5 Відкриття модального вікна деталей завдання

- **Актор(и):** Board Member, Board Owner
- **Опис:** Користувач натискає на картку завдання в колонці — відкривається модальне вікно з повними деталями завдання, коментарями та Quick Actions.
- **Локація в коді:** `Pages/Board/Components/TaskDetailsModal.razor`, `Pages/Board/Components/TaskViewMode.razor`
- **Взаємодія користувача (Покроково):**
  1. Клік по картці → метод `OnTaskClick.InvokeAsync(task)` у `BoardColumns` → відкривається `TaskDetailsModal` (шириною 1200px).
  2. Ліва панель модального вікна (md:8) відображає `TaskViewMode`: назва, стан (синій Tag), опис, тег із кольором, assignee з аватаркою, due date, пріоритет.
  3. Під детальною інформацією знаходяться `TaskQuickActions` — блок кнопок для швидкої зміни стану.
  4. Права панель (md:16) відображає `TaskCommentsComponent` із списком коментарів, полем пошуку і формою нового коментаря.
  5. У футері модального вікна (`TaskModalFooter`): кнопки «Close», «Edit Task», «Delete Task» (Popconfirm), «Archive Task» (Popconfirm) — видимі лише якщо `CanManageTask == true`.
- **Виклик API:** `ITaskService.GetTaskByIdAsync(taskId)` для завантаження деталей завдання; `ICommentService.GetCommentsAsync(taskId)` для коментарів

---

### 1.6 Quick Actions — швидке переміщення завдання між станами

- **Актор(и):** Board Member, Board Owner
- **Опис:** У модальному вікні завдання під полем перегляду знаходиться блок «Quick Actions», який дозволяє одним кліком перемістити завдання в інший стан.
- **Локація в коді:** `Pages/Board/Components/TaskQuickActions.razor`
- **Взаємодія користувача (Покроково):**
  1. Блок `Quick Actions` відображається у нижній частині лівої панелі `TaskDetailsModal`.
  2. Відображаються кнопки для кожного стану дошки; поточний стан виділений як `ButtonType.Primary`.
  3. Кнопки неактивних станів мають тип `Default`; кнопка поточного стану `Disabled`.
  4. Клік по кнопці іншого стану → `OnStateChange.InvokeAsync(stateId)` → стан завдання змінюється без закриття модалки.
  5. Список карток у колонках оновлюється, кнопка нового поточного стану стає Primary.
- **Виклик API:** `ITaskService.UpdateTaskAsync(taskId, updateRequest)` де `StateId` = обраний стан

---

### 1.7 Редагування завдання (Task Edit Mode)

- **Актор(и):** Board Owner, Board Member (якщо `CanManageTask`)
- **Опис:** Користувач натискає кнопку «Edit Task» у футері `TaskDetailsModal` — ліва панель перемикається в режим редагування форми.
- **Локація в коді:** `Pages/Board/Components/TaskEditMode.razor`, `Pages/Board/Components/TaskDetailsModal.razor`
- **Взаємодія користувача (Покроково):**
  1. Клік «Edit Task» → `IsEditing = true` у `TaskDetailsModal`.
  2. `TaskViewMode` та `TaskQuickActions` замінюються на `TaskEditMode` — форму з полями: Title, Description, State (Select), Assignee (Select), Due Date (DatePicker), Priority (Select), Tag (Select).
  3. Права панель з коментарями зникає (прихована у режимі редагування).
  4. Футер змінюється: показуються кнопки «Cancel» та «Save Changes».
  5. Клік «Save Changes» → валідація форми → збереження → `IsEditing = false`, модалка повертається до view-режиму.
- **Виклик API:** `ITaskService.UpdateTaskAsync(taskId, updateRequest)` → `IBoardTaskApi`

---

### 1.8 Видалення та архівування завдання

- **Актор(и):** Board Owner, Board Member (якщо `CanManageTask`)
- **Опис:** У футері `TaskDetailsModal` є кнопки «Delete Task» та «Archive Task», кожна з яких вимагає підтвердження через `Popconfirm`.
- **Локація в коді:** `Pages/Board/Components/TaskModalFooter.razor`
- **Взаємодія користувача (Покроково):**
  1. Клік «Delete Task» → з'являється `Popconfirm` з питанням підтвердження.
  2. Клік «Yes» → завдання видаляється, модальне вікно закривається, картка зникає з колонки.
  3. Клік «Archive Task» → аналогічний `Popconfirm` → завдання переміщується в бекілог, зникає з board view.
  4. Клік «No» в обох випадках закриває Popconfirm без дії.
- **Виклик API:** `ITaskService.DeleteTaskAsync(taskId)` або `ITaskService.ArchiveTaskAsync(taskId)` → `IBoardTaskApi`

---

### 1.9 Створення нового завдання

- **Актор(и):** Board Owner, Board Member
- **Опис:** Користувач натискає кнопку «Add New Task» у хедері дошки — відкривається модальне вікно форми створення завдання.
- **Локація в коді:** `Pages/Board/Components/AddTaskModal.razor`, `Pages/Board/Components/BoardHeader.razor`
- **Взаємодія користувача (Покроково):**
  1. Кнопка `+ Add New Task` (Primary, іконка `plus`) знаходиться у крайньому правому куті `BoardHeader`.
  2. Клік → відкривається `AddTaskModal` (шириною 600px) з формою.
  3. Поля форми: Title (обов'язкове, max 200), Description (textarea, max 1000), State (Select — список станів дошки, обов'язкове), Assignee (Select — учасники дошки), Due Date (Input type=date), Priority (Select: Low/Normal/High, обов'язкове), Tag (Select — теги дошки).
  4. Кнопка «OK» виконує валідацію — у разі помилки показуються повідомлення під полями.
  5. При успіху модалка закривається, нова картка завдання з'являється у відповідній колонці стану.
- **Виклик API:** `ITaskService.CreateTaskAsync(boardId, createRequest)` → `IBoardTaskApi`

---

### 1.10 Налаштування станів дошки (Manage States)

- **Актор(и):** Board Owner
- **Опис:** Board Owner натискає кнопку «States» у хедері дошки — відкривається модальне вікно управління станами (колонками).
- **Локація в коді:** `Pages/Board/Components/ManageStatesModal.razor`, `Pages/Board/Components/AddStateModal.razor`
- **Взаємодія користувача (Покроково):**
  1. Клік «States» (іконка `setting`) у `BoardHeader` → відкривається `ManageStatesModal` (шириною 600px).
  2. Таблиця відображає всі стани з колонками: Order (кнопки ↑/↓), Name (редаговане Input-поле), Actions (кнопка Delete).
  3. Зміна порядку: кнопки ↑/↓ міняють стани місцями; кнопки disabled для першого та останнього елементів відповідно.
  4. Редагування назви: клік на поле Input → введення нової назви → `OnBlur` зберігає зміни.
  5. Видалення стану: клік «Delete» → стан видаляється з таблиці та з колонок дошки.
  6. Кнопка «+ Add State» відкриває вкладений `AddStateModal` з формою назви нового стану.
- **Виклик API:** `ITaskStateService.CreateStateAsync(...)`, `ITaskStateService.UpdateStateAsync(...)`, `ITaskStateService.DeleteStateAsync(...)`, `ITaskStateService.SwapStateOrderAsync(...)` → `IBoardStateApi`

---

### 1.11 Управління тегами дошки (Manage Tags)

- **Актор(и):** Board Owner
- **Опис:** Board Owner натискає кнопку «Tags» у хедері — відкривається модальне вікно для створення, редагування та видалення тегів.
- **Локація в коді:** `Pages/Board/Components/ManageTagsModal.razor`, `Pages/Board/Components/AddTagModal.razor`
- **Взаємодія користувача (Покроково):**
  1. Клік «Tags» (іконка `tag`) у `BoardHeader` → відкривається `ManageTagsModal`.
  2. Відображається список тегів із назвою та кольором.
  3. Кнопка «+ Add Tag» відкриває `AddTagModal` — форма з полями Name та Color picker.
  4. Кожен тег має кнопки Edit та Delete у таблиці.
  5. Теги відразу відображаються на картках завдань у колонках.
- **Виклик API:** `ITagService.CreateTagAsync(...)`, `ITagService.UpdateTagAsync(...)`, `ITagService.DeleteTagAsync(...)` → `ITagApi`

---

### 1.12 Управління учасниками дошки (Members Modal)

- **Актор(и):** Board Owner (управління), Board Member (перегляд)
- **Опис:** Кнопка «Members» у хедері відкриває модальне вікно зі списком учасників та можливістю додати нового або змінити роль.
- **Локація в коді:** `Pages/Board/Components/MembersModal.razor`, `Pages/Board/Components/AddMemberModal.razor`
- **Взаємодія користувача (Покроково):**
  1. Клік «Members» (іконка `team`) у `BoardHeader` → відкривається `MembersModal` (шириною 800px).
  2. Список учасників відображає: аватарку (або ініціали), ім'я, email, роль (Member/Owner), дату приєднання.
  3. Board Owner бачить кнопки «Make Member», «Make Owner» та «Remove» навпроти кожного учасника (крім себе).
  4. Клік «+ Add Member» → відкривається `AddMemberModal` з Select-полем для вибору користувача з організації та вибором ролі.
  5. Після додавання список учасників оновлюється.
- **Виклик API:** `IBoardMemberService.AddMemberAsync(...)`, `IBoardMemberService.UpdateMemberRoleAsync(...)`, `IBoardMemberService.RemoveMemberAsync(...)` → `IBoardMemberApi`

---

### 1.13 Створення нової дошки

- **Актор(и):** User
- **Опис:** Користувач натискає кнопку «+ Create Board» на сторінці списку дошок — відкривається модальне вікно форми створення.
- **Локація в коді:** `Pages/Boards/Components/CreateBoardModal.razor`, `Pages/Boards/Boards.razor`
- **Взаємодія користувача (Покроково):**
  1. Кнопка «+ Create Board» знаходиться у `BoardsPageHeader`.
  2. Клік → відкривається `CreateBoardModal` (шириною 500px).
  3. Форма містить: `OrganizationSelector` (обов'язковий, виключає гостьові організації), Name (обов'язкове, max 100), Description (textarea, max 500).
  4. При помилці валідації або API-відповіді — `Alert` з текстом помилки з'являється всередині модалки.
  5. При успіху модалка закривається, нова картка з'являється у списку дошок.
- **Виклик API:** `IBoardService.CreateBoardAsync(createRequest)` → `IBoardApi`

---

### 1.14 Видалення дошки

- **Актор(и):** Board Owner
- **Опис:** Board Owner натискає іконку «Delete» на картці дошки — відкривається модальне вікно підтвердження видалення.
- **Локація в коді:** `Pages/Boards/Components/DeleteBoardModal.razor`, `Pages/Boards/Components/BoardSearchCard.razor`
- **Взаємодія користувача (Покроково):**
  1. На картці дошки Board Owner бачить кнопку видалення (одна з дій картки `GetCardActions()`).
  2. Клік → відкривається `DeleteBoardModal` з вимогою ввести підтвердний текст (назву дошки або ключове слово).
  3. Кнопка «Delete» активується лише після правильного введення `_deleteConfirmation`.
  4. При успіху модалка закривається, картка зникає зі списку.
- **Виклик API:** `IBoardService.DeleteBoardAsync(boardId)` → `IBoardApi`

---

### 1.15 Архівування дошки

- **Актор(и):** Board Owner
- **Опис:** Board Owner натискає кнопку «Archive Board» у хедері сторінки дошки — відкривається модальне вікно підтвердження.
- **Локація в коді:** `Pages/Board/Components/ArchiveBoardModal.razor`, `Pages/Board/Components/BoardHeader.razor`
- **Взаємодія користувача (Покроково):**
  1. Кнопка «Archive Board» (Danger, іконка `delete`) бачима лише Board Owner у `BoardHeader`.
  2. Клік → відкривається `ArchiveBoardModal` з попередженням.
  3. Підтвердження → дошка переходить в архівований стан.
  4. У списку дошок архівована дошка відображається лише при фільтрі «Archived» з кнопкою «Dearchive».
- **Виклик API:** `IBoardService.ArchiveBoardAsync(boardId)` → `IBoardApi`

---

### 1.16 Перегляд бекілогу (Backlog)

- **Актор(и):** Board Member, Board Owner
- **Опис:** Користувач натискає кнопку «Backlog» у хедері дошки і потрапляє на окрему сторінку з архівованими завданнями.
- **Локація в коді:** `Pages/Board/Backlog.razor`
- **Взаємодія користувача (Покроково):**
  1. Клік «Backlog» (іконка `ordered-list`) у `BoardHeader` → навігація до `/board/{boardId}/backlog`.
  2. Сторінка відображає `PageHeader` з назвою «Backlog for [Board Name]» та кнопкою назад.
  3. Вгорі розміщені фільтри: поле пошуку (Input з `AllowClear`), DatePicker для Start Date та End Date.
  4. Список (`AntList`) відображає архівовані завдання з описом та датою архівації.
  5. Якщо є більше записів — з'являється кнопка «Load more» для підвантаження.
  6. Зміна дат у DatePicker-ах миттєво перезавантажує список через `OnDateChanged`.
- **Виклик API:** `ITaskService.GetBacklogAsync(boardId, searchTerm, startDate, endDate, page)` → `IBoardTaskApi`

---

### 1.17 Перегляд та налаштування зустрічей дошки (Manage Meetings)

- **Актор(и):** Board Owner (створення/редагування/видалення), Board Member (перегляд та перехід до дзвінка)
- **Опис:** Кнопка «Meetings» у хедері дошки відкриває модальне вікно з переліком запланованих зустрічей.
- **Локація в коді:** `Pages/Board/Components/ManageMeetingsModal.razor`, `Pages/Board/Components/AddMeetingModal.razor`
- **Взаємодія користувача (Покроково):**
  1. Клік «Meetings» (іконка `phone`) у `BoardHeader` → відкривається `ManageMeetingsModal` (шириною 900px).
  2. Список зустрічей відображає: назву, статус (кольоровий Tag: Upcoming / In Progress / Completed), опис, час початку та кінця, тривалість, посилання.
  3. Board Owner бачить кнопки «Edit» та «Delete» (Popconfirm) навпроти кожної зустрічі.
  4. Клік «+ Schedule New Meeting» або «Schedule First Meeting» → відкривається `AddMeetingModal`.
  5. Клік «Join Meeting» або на посилання → перехід на `BoardCallPage` (`/board/{boardId}/meeting/{meetingId}`).
- **Виклик API:** `IMeetingService.GetMeetingsAsync(boardId)`, `IMeetingService.CreateMeetingAsync(...)`, `IMeetingService.UpdateMeetingAsync(...)`, `IMeetingService.DeleteMeetingAsync(...)` → `IMeetingApi`

---

## Секція 2 — UI: Комунікація та сповіщення

---

### 2.1 Підключення до відеодзвінка (BoardCallPage)

- **Актор(и):** Board Member, Board Owner
- **Опис:** Користувач переходить на сторінку відеоконференції за посиланням зустрічі та підключається до дзвінка з учасниками дошки.
- **Локація в коді:** `Pages/Board/BoardCallPage.razor`
- **Взаємодія користувача (Покроково):**
  1. Навігація до `/board/{boardId}/meeting/{meetingId}` — завантажується `BoardCallPage`.
  2. Статус з'єднання відображається у `Tag` у хедері: «Connecting…» (помаранчевий) → «Ready» (синій) → «In call with N» (зелений).
  3. До початку дзвінка відображається панель попереднього перегляду локального відео «You (Preview)» з індикаторами камери/мікрофона.
  4. Блок керування містить кнопки:
	 - **Start Call** (Primary) — ввімкнути дзвінок; після підключення стає неактивною з текстом «Connected».
	 - **Camera On/Off** — перемикає відео; Primary якщо увімкнено.
	 - **Mic On/Off** — перемикає мікрофон; Primary якщо увімкнено; іконка `audio`/`audio-muted`.
	 - **Share Screen / Stop Share** — перемикає демонстрацію екрана.
	 - **Hang Up** (Danger, Primary) — завершити дзвінок; активна лише під час дзвінка.
  5. Під час дзвінка відображаються відеострими усіх учасників з іменами та індикаторами стану медіа.
- **Виклик API:** `IMeetingMemberService` для управління учасниками; WebRTC/Agora через JavaScript interop (`boardcall.js`)

---

### 2.2 Перемикання камери під час дзвінка

- **Актор(и):** Board Member, Board Owner
- **Опис:** Під час активного сеансу у `BoardCallPage` користувач вмикає або вимикає камеру.
- **Локація в коді:** `Pages/Board/BoardCallPage.razor`, `wwwroot/js/boardcall.js`
- **Взаємодія користувача (Покроково):**
  1. Клік кнопки «Camera On» → камера вимикається, кнопка стає `Default` («Camera Off»), у відеострімі з'являється червоний Tag «Off».
  2. Повторний клік → камера вмикається, кнопка повертається до `Primary`.
  3. Зміна стану камери транслюється у реальному часі іншим учасникам через WebRTC.
- **Виклик API:** JavaScript interop через `boardcall.js` (Agora SDK)

---

### 2.3 Перегляд та взаємодія з NotificationToast

- **Актор(и):** User
- **Опис:** При виникненні нової події (додавання до дошки, призначення на завдання, коментар) у нижньому кутку екрана з'являється toast-сповіщення.
- **Локація в коді:** `Components/Shared/NotificationToast.razor`, `Layouts/BasicLayout.razor`
- **Взаємодія користувача (Покроково):**
  1. SignalR-подія від сервера → компонент `NotificationToast` стає видимим (CSS клас `show`).
  2. Toast відображає іконку типу сповіщення (`team` / `user` / `message`), заголовок «New Notification» та текст повідомлення.
  3. Клік по тосту → `OnClick` → навігація до відповідного ресурсу (дошки або завдання).
  4. Клік по іконці ✕ (`notification-close`) → `OnClose` → тост приховується без навігації.
  5. Тост автоматично зникає через певний час якщо на нього не реагують.
- **Виклик API:** `INotificationSignalRService` (SignalR subscription); `INotificationService.MarkAsReadAsync(notificationId)` після кліку

---

### 2.4 Перегляд повного списку сповіщень

- **Актор(и):** User
- **Опис:** Користувач переходить до `/notifications` для перегляду всієї історії сповіщень з можливістю позначити всі як прочитані.
- **Локація в коді:** `Pages/Notifications.razor`
- **Взаємодія користувача (Покроково):**
  1. Навігація до `/notifications` (або клік у боковому меню).
  2. `PageHeader` з кнопками «Mark All as Read» та «Refresh».
  3. Список `AntList` відображає кожне сповіщення: кольорова аватарка-іконка за типом, жирний заголовок для непрочитаних із `Badge` (синій пульсуючий), текст та дата.
  4. Непрочитані мають жирний текст; прочитані — сірий колір.
  5. Клік «Mark All as Read» → всі записи переходять у прочитаний стан, кнопка стає неактивною.
  6. Клік «Refresh» → повторне завантаження з API.
- **Виклик API:** `INotificationService.GetNotificationsAsync()`, `INotificationService.MarkAllAsReadAsync()`, `INotificationService.MarkAsReadAsync(id)` → `INotificationApi`

---

### 2.5 Відправка запиту до AI-асистента

- **Актор(и):** User
- **Опис:** Користувач переходить на сторінку `/ai-assistant`, обирає організацію, вводить питання та отримує відповідь у форматі Markdown.
- **Локація в коді:** `Pages/AiAssistant.razor`
- **Взаємодія користувача (Покроково):**
  1. Навігація до `/ai-assistant`.
  2. `OrganizationSelector` — обов'язковий вибір організації для контексту запиту.
  3. `TextArea` (4 рядки) — введення питання (кнопка «Ask» неактивна поки поле порожнє або організацію не обрано).
  4. Клік «Ask» → кнопка показує `Loading`; відображається спінер «Thinking…».
  5. При успіху → відповідь рендериться як форматований HTML через `Markdig` у картці «AI Response».
  6. При помилці → `Alert` типу Error з описом помилки та кнопкою закриття.
  7. Повторний запит очищає попередню відповідь і починає новий цикл.
- **Виклик API:** `IChatSystemService.AskAiAsync(organizationId, question)` → `IChatSystemApi`

---

### 2.6 Перегляд коментарів завдання та пошук у них

- **Актор(и):** Board Member, Board Owner
- **Опис:** Права панель модального вікна `TaskDetailsModal` містить список коментарів із пошуком.
- **Локація в коді:** `Pages/Board/Components/TaskCommentsComponent.razor`
- **Взаємодія користувача (Покроково):**
  1. Відкриття `TaskDetailsModal` → права панель автоматично завантажує коментарі.
  2. Поле `Search` (Input) з `AllowClear` у верхній частині панелі — введення тексту фільтрує коментарі за вмістом.
  3. Кожен коментар показує аватарку (або ініціали), ім'я автора, дату та текст.
  4. Автор коментаря бачить кнопку «⋯» → Dropdown з пунктами «Edit» та «Delete (Danger)».
  5. Клік «Edit» → TextArea з поточним вмістом, кнопки «Cancel» та «Save».
  6. Клік «Delete» → підтвердження (Popconfirm) → коментар видаляється зі списку.
- **Виклик API:** `ICommentService.GetCommentsAsync(taskId)`, `ICommentService.UpdateCommentAsync(...)`, `ICommentService.DeleteCommentAsync(...)` → `ICommentApi`

---

### 2.7 Додавання нового коментаря до завдання

- **Актор(и):** Board Member, Board Owner (якщо `CanAddComment`)
- **Опис:** У нижній частині секції коментарів знаходиться форма введення нового коментаря з підтримкою вкладення файлів.
- **Локація в коді:** `Pages/Board/Components/TaskCommentsComponent.razor`, `Pages/Board/Components/CommentAttachments.razor`
- **Взаємодія користувача (Покроково):**
  1. Поле `TextArea` у нижній частині правої панелі — введення тексту коментаря.
  2. Кнопка прикріплення файлів (скріпка) відкриває системний діалог вибору файлів.
  3. Обрані файли відображаються у списку `CommentAttachments` — зображення показуються як мініатюри, інші файли — як посилання з іконкою 📎.
  4. Клік на мініатюру зображення → відкривається `Modal` попереднього перегляду (шириною 800px).
  5. Кнопка «Send» → коментар надсилається з прикріпленими файлами; форма очищається.
- **Виклик API:** `IAttachmentService.UploadAttachmentAsync(file)` → `IAttachmentApi`; `ICommentService.CreateCommentAsync(taskId, createRequest)` → `ICommentApi`

---

### 2.8 Взаємодія у Chat

- **Актор(и):** User
- **Опис:** Користувач відкриває `Chat.razor`, обирає або створює чат, переглядає повідомлення та надсилає нові.
- **Локація в коді:** `Pages/Chat/Chat.razor`, `Pages/Chat/Components/ChatList.razor`, `Pages/Chat/Components/ChatMessages.razor`
- **Взаємодія користувача (Покроково):**
  1. Навігація до `/chat` → двохпанельний layout: зліва `ChatList`, справа `ChatMessages`.
  2. `ChatList` відображає список чатів з аватарками, іменами та превʼю останнього повідомлення; непрочитані — виділені.
  3. Клік на чат → праворуч завантажуються повідомлення.
  4. Поле введення в нижній частині `ChatMessages` — введення тексту та натискання Enter або кнопки «Send».
  5. Підтримується завантаження файлів-вкладень через `ChatMessageAttachments`.
  6. Індикатор набору тексту (`typing indicator`) відображається коли інший учасник пише.
- **Виклик API:** `IChatService.GetChatsAsync()`, `IChatService.SendMessageAsync(...)`, `IChatSignalRService` (SignalR real-time) → `IChatApi`

---

### 2.9 Запрошення учасників (Invitations)

- **Актор(и):** User
- **Опис:** Користувач переходить до `/invitations` для перегляду вхідних запрошень до дошок або організацій та прийняття/відхилення їх.
- **Локація в коді:** `Pages/Invitations.razor`
- **Взаємодія користувача (Покроково):**
  1. Навігація до `/invitations`.
  2. Відображаються списки запрошень до дошок (`BoardInvitationDto`) та організацій (`OrganizationInvitationDto`).
  3. Кожне запрошення має кнопки «Accept» та «Decline».
  4. Клік «Accept» → користувач додається до дошки/організації, запрошення зникає зі списку.
  5. Клік «Decline» → запрошення відхиляється та зникає.
- **Виклик API:** `IInvitationService.GetPendingInvitationsAsync()`, `IInvitationService.AcceptInvitationAsync(...)`, `IInvitationService.DeclineInvitationAsync(...)` → `IInvitationApi`

---

### 2.10 Перегляд Календаря

- **Актор(и):** User
- **Опис:** Користувач переходить до `/calendar` для перегляду завдань із дедлайнами та запланованих зустрічей у вигляді календаря.
- **Локація в коді:** `Pages/Calendar.razor`
- **Взаємодія користувача (Покроково):**
  1. Навігація до `/calendar`.
  2. Відображається календарний компонент із позначками завдань (`TaskCalendarItemDto`) та зустрічей (`MeetingCalendarItemDto`).
  3. Клік на подію → деталі задачі або зустрічі.
  4. Можлива інтеграція з Google Calendar через кнопку підключення (`IGoogleCalendarService`).
- **Виклик API:** `ITaskService.GetTaskCalendarItemsAsync(...)`, `IMeetingService.GetMeetingCalendarItemsAsync(...)`, `IGoogleCalendarService` → відповідні Api-інтерфейси
