namespace UI.Resources;


public static class I18n
{
    // Login / Welcome
    public const string SignInToTaskPilot = "Увійти в TaskPilot";
    public const string WelcomeToTaskPilot = "Ласкаво просимо в TaskPilot";
    public const string SignInWithMicrosoft = "Увійти через Microsoft";
    public const string SigningYouIn = "Виконується вхід...";
    public const string AuthenticationError = "Помилка автентифікації";
    public const string AuthenticationFailedWithError = "Не вдалося автентифікуватися: {0}";
    public const string FailedToInitiateLogin = "Не вдалося ініціювати вхід: {0}";
    public const string AuthenticationFailedPleaseCheckConsole = "Не вдалося автентифікуватися. Будь ласка, перевірте консоль для деталей.";
    public const string AuthenticationException = "Помилка автентифікації: {0}";
    public const string SecureAuthPoweredByAzure = "Безпечна автентифікація через Azure AD";
    public const string SecureSingleSignOn = "Безпечний єдиний вхід";
    public const string EnterpriseGradeSecurity = "Рівень безпеки для підприємств";
    public const string TeamCollaborationReady = "Готово для командної співпраці";

    // Welcome page
    public const string WelcomeBack = "Раді вас знову бачити!";
    public const string Dashboard = "Панель керування";
    public const string UserInformation = "Інформація про користувача";
    public const string Username = "Ім'я користувача:";
    public const string Email = "Електронна пошта:";
    public const string Role = "Роль:";
    public const string MemberSince = "Учасник з:";
    public const string CreateNewBoard = "Створити дошку";
    public const string ViewMyBoards = "Мої дошки";
    public const string ProfileSettings = "Налаштування профілю";
    public const string NoRecentActivity = "Немає останньої активності";
    public const string Logout = "Вийти";
    public const string SignIn = "Увійти";
    public const string LoadingUserInformation = "Завантаження інформації про користувача...";

    // Boards
    public const string CreateYourFirstBoard = "Створіть вашу першу дошку";
    public const string LoadMoreBoards = "Завантажити ще дошок";
    public const string LoadingYourBoards = "Завантаження ваших дошок...";
    public const string LoadingMoreBoards = "Завантаження ще дошок...";
    public const string ClearFilters = "Скинути фільтри";
    public const string SearchYourBoards = "Пошук по дошках...";
    public const string Show = "Показати:";
    public const string AllBoards = "Усі дошки";
    public const string Owner = "Власник";
    public const string Member = "Учасник";
    public const string Archived = "Архівні";
    // Priority labels / generic
    public const string PriorityLowLabel = "Низький";
    public const string PriorityNormalLabel = "Нормальний";
    public const string PriorityHighLabel = "Високий";
    public const string PriorityImmediateLabel = "Негайний";
    public const string UserLabel = "Користувач";
    public const string UnknownLabel = "Невідомо";
    // Attachments / preview
    public const string AttachmentPreviewTitle = "Попередній перегляд вкладення";
    public const string PreviewAlt = "Попередній перегляд";
    // Boards header
    public const string MyBoardsTitle = "Мої дошки";
    public const string Refresh = "Оновити";
    public const string CreateBoard = "Створити дошку";
    public const string DeleteBoardConfirmationMismatch = "Підтвердження видалення не збігається з назвою дошки.";
    public const string NoBoardsFoundMatching = "Дошки не знайдені за запитом \"{0}\"";
    public const string YouDontOwnAnyBoardsYetCreateFirst = "Ви ще не володієте жодною дошкою. Створіть першу дошку, щоб почати!";
    public const string YouAreNotMemberOfAnyBoards = "Ви ще не є учасником жодної дошки.";
    public const string YouDontHaveAnyBoardsCreateFirst = "У вас ще немає дошок. Створіть першу дошку, щоб почати!";
    public const string NoBoardsFound = "Дошок не знайдено";
    public const string YearsAgo = "{0} р. тому";
    public const string MonthsAgo = "{0} міс. тому";
    public const string RefreshToSeeChanges = "Оновіть, щоб побачити зміни";
    // Calendar
    public const string CalendarTitle = "Календар";
    public const string Today = "Сьогодні";
    public const string LoadingYourCalendar = "Завантаження вашого календаря...";
    public const string ItemsFor = "Пункти за {0}";
    public const string NoTasksOrMeetingsForDate = "Немає завдань або зустрічей на цю дату";
    public const string MonthlyOverview = "Місячний огляд";
    public const string TotalTasks = "Всього завдань";
    public const string TotalMeetings = "Всього зустрічей";
    public const string OverdueTasks = "Прострочені завдання";
    // Task quick actions
    public const string MoveToStateLabel = "Перемістити в стан:";
    public const string QuickActionsLabel = "Швидкі дії";
    public const string StartDateLabel = "Дата початку:";
    public const string EndDateLabel = "Дата завершення:";

    // Create board modal
    public const string CreateBoardOk = "Створити дошку";
    public const string BoardNameLabel = "Назва дошки";
    public const string EnterBoardName = "Введіть назву дошки";
    public const string DescriptionLabel = "Опис";
    public const string EnterBoardDescription = "Введіть опис дошки (необов'язково)";
    public const string NoDescription = "Немає опису";

    // Board header / actions
    public const string Loading = "Завантаження...";
    public const string ArchiveBoard = "Архівувати дошку";
    public const string Backlog = "Беклог";
    public const string States = "Стан/Стани";
    public const string Tags = "Теги";
    public const string Meetings = "Зустрічі";
    public const string EnterMeetingTitle = "Введіть заголовок зустрічі";
    public const string EnterMeetingDescription = "Введіть опис зустрічі (необов'язково)";
    public const string MeetingDurationPlaceholder = "Тривалість зустрічі у хвилинах";
    public const string SelectMeetingAttendees = "Оберіть учасників зустрічі";
    public const string ScheduleNewMeeting = "Запланувати нову зустріч";
    public const string NoMeetingsScheduled = "Немає запланованих зустрічей для цієї дошки";
    public const string ScheduleFirstMeeting = "Запланувати першу зустріч";
    public const string ConfirmDeleteMeeting = "Ви впевнені, що хочете видалити цю зустріч?";
    public const string EditMeeting = "Редагувати зустріч";
    public const string SelectMeetingDateTime = "Оберіть дату та час зустрічі";
    public const string MinutesLabel = "{0} хвилин";
    public const string PleaseEnterTaskTitle = "Будь ласка, введіть заголовок завдання";
    public const string PleaseSelectState = "Будь ласка, виберіть стан для завдання";
    public const string TaskCreatedSuccess = "Завдання '{0}' успішно створено";
    public const string FailedToCreateTask = "Не вдалося створити завдання: {0}";
    public const string OnlyOwnersCanScheduleMeetings = "Тільки власники дошки та учасники можуть запланувати зустріч";
    public const string PleaseEnterMeetingTitle = "Будь ласка, введіть заголовок зустрічі";
    public const string PleaseEnterMeetingDomain = "Будь ласка, введіть домен зустрічі";
    public const string MeetingScheduledSuccess = "Зустріч '{0}' успішно запланована";
    public const string FailedToScheduleMeeting = "Не вдалося запланувати зустріч: {0}";
    public const string MeetingUpdatedSuccess = "Зустріч успішно оновлена";
    public const string MeetingDeletedSuccess = "Зустріч успішно видалена";
    public const string FailedToUpdateMeeting = "Не вдалося оновити зустріч: {0}";
    public const string FailedToDeleteMeeting = "Не вдалося видалити зустріч: {0}";
    public const string FailedToLoadUsers = "Не вдалося завантажити користувачів";
    public const string FailedToLoadMeetings = "Не вдалося завантажити зустрічі";
    public const string FailedToConnectNotificationService = "Не вдалося підключитися до служби повідомлень: {0}";
    public const string FailedToLoadNotifications = "Не вдалося завантажити повідомлення: {0}";
    public const string NotificationsRefreshed = "Повідомлення оновлено";
    public const string AllNotificationsMarkedRead = "Усі повідомлення позначено як прочитані";
    public const string FailedToMarkNotificationRead = "Не вдалося позначити повідомлення як прочитане: {0}";
    public const string FailedToDeleteNotification = "Не вдалося видалити повідомлення: {0}";

    // Notification titles
    public const string NotificationAddedToBoard = "Додано до дошки";
    public const string NotificationTaskAssignment = "Призначено завдання";
    public const string NotificationNewComment = "Новий коментар";
    public const string NotificationDefault = "Повідомлення";
    public const string MeetingStatusUpcoming = "Найближча";
    public const string MeetingStatusInProgress = "В процесі";
    public const string MeetingStatusCompleted = "Завершена";
    public const string MeetingStatusUnscheduled = "Не заплановано";
    public const string DurationMinutesLabel = "Тривалість (хвилини)";
    public const string AttendeesLabel = "Учасники";
    public const string AIAssistantTitle = "Помічник AI для TaskPilot";
    public const string Thinking = "Думаю...";
    public const string MeetingTitleLabel = "Назва зустрічі";
    public const string BacklogFor = "Беклог для \"{0}\"";
    public const string MarkAsRead = "Позначити як прочитане";
    public const string AttachText = "Прикріпити";
    public const string BoardMeetingTitle = "Зустріч на дошці";
    public const string Connecting = "Підключення...";
    public const string InCallWithCount = "У дзвінку ({0} користувачів)";
    public const string Ready = "Готово";
    public const string StartCall = "Почати дзвінок";
    public const string Connected = "Підключено";
    public const string CameraOn = "Камера увімкнена";
    public const string CameraOff = "Камера вимкнена";
    public const string MicOn = "Мікрофон увімкнений";
    public const string MicOff = "Мікрофон вимкнений";
    public const string StopShare = "Зупинити показ екрану";
    public const string ShareScreen = "Показати екран";
    public const string HangUp = "Завершити дзвінок";
    public const string VideoPreviewTitle = "Попередній перегляд відео";
    public const string ClickStartCall = "Натисніть 'Почати дзвінок' щоб розпочати відеоконференцію";
    public const string VideoConferenceTitle = "Відеоконференція";
    public const string YouPreview = "Ви (попередній перегляд)";
    public const string Off = "Вимкнено";
    public const string Screen = "Екран";
    public const string ConnectingUpper = "ПІДКЛЮЧАЄТЬСЯ";
    public const string Online = "ОНЛАЙН";
    public const string Retry = "СПРОБУЙТЕ ЗНОВУ";
    public const string OffLabel = "ВИМК";
    public const string ScreenLabel = "ЕКРАН";
    public const string View = "Переглянути";
    public const string LoadingBoard = "Завантаження дошки...";
    public const string NoTasksInState = "Немає завдань у цьому стані";
    public const string BoardNotFoundTitle = "Дошку не знайдено";
    public const string BoardNotFoundDescription = "Дошку, яку ви шукаєте, не знайдено або у вас немає доступу.";
    public const string GoBack = "Повернутись";
    public const string Dearchive = "Розархівувати";
    public const string MyTasks = "Мої завдання";
    public const string AllTasks = "Усі завдання";
    public const string Description = "Опис";
    public const string TasksLabel = "Завдання";
    public const string MembersLabel = "Учасники";
    
    // Task view labels
    public const string TagLabel = "Тег:";
    public const string AssigneeLabel = "Виконавець:";
    public const string PriorityLabel = "Пріоритет:";
    public const string DueLabel = "Термін:";
    public const string NoTagText = "Без тегу";
    public const string NotAssigned = "Не призначено";
    public const string NoDueDate = "Без терміну";
    public const string CreatedLabel = "Створено";
    public const string UpdatedLabel = "Оновлено";

    // Notifications
    public const string TaskArchivedSuccess = "Завдання успішно архівовано!";
    public const string TaskUpdatedSuccess = "Завдання успішно оновлено!";
    public const string TaskDeletedSuccess = "Завдання успішно видалено!";
    public const string Success = "Успішно";
    public const string Unknown = "Невідомо";
    public const string TaskMovedSuccess = "Завдання переміщено в {0} успішно!";
    
    // General errors / status
    public const string Error = "Помилка";
    public const string FailedToLoadComments = "Не вдалося завантажити коментарі";
    public const string FailedToAddComment = "Не вдалося додати коментар";
    public const string CommentUpdatedSuccess = "Коментар успішно оновлено";
    public const string FailedToUpdateComment = "Не вдалося оновити коментар";
    public const string CommentDeletedSuccess = "Коментар успішно видалено";
    public const string FailedToDeleteComment = "Не вдалося видалити коментар";
    public const string UnknownUser = "Невідомий користувач";
    public const string You = "Ви";
    public const string JustNow = "Щойно";
    public const string MinutesAgo = "{0}хв тому";
    public const string HoursAgo = "{0}год тому";
    public const string DaysAgo = "{0}дн тому";
    
    // Delete board modal
    public const string DeleteBoardTitle = "Видалити дошку";
    public const string DeleteBoardWarning = "Ця дія не може бути скасована.";
    public const string DeleteBoardDescription = "Видалення дошки назавжди видалить всі завдання, коментарі та пов'язані дані. Будь ласка, введіть назву дошки для підтвердження.";
    // UI common
    public const string LoadingDots = "Завантаження...";
    public const string NewNotification = "Нове повідомлення";
    // Manage states/tags
    public const string ManageStates = "Керування станами";
    public const string AddStateButton = "Додати стан";
    public const string OrderLabel = "Порядок";
    public const string NameLabel = "Назва";
    public const string ActionsLabel = "Дії";
    public const string DeleteLabel = "Видалити";
    public const string ManageTags = "Керування тегами";
    public const string AddTagButton = "Додати тег";
    public const string ColorLabel = "Колір";
    public const string BoardMembersTitle = "Учасники дошки";
    public const string AddMemberButton = "Додати учасника";
    public const string LoadingMemberDetails = "Завантаження деталей учасників...";
    public const string NoMembersFound = "Учасників не знайдено";
    public const string MakeMember = "Зробити учасником";
    public const string MakeOwner = "Зробити власником";
    public const string RemoveMember = "Видалити";
    // Task details
    public const string TaskDetailsTitle = "Деталі завдання - {0}";
    public const string LoadingTaskDetails = "Завантаження деталей завдання...";
    // Board call / meeting
    public const string BoardMeetingPageTitle = "Зустріч на дошці - {0}";
    public const string UserNotAuthenticated = "Користувач не автентифікований.";
    public const string InvalidMeetingIdFormat = "Неправильний формат MeetingId.";
    public const string ErrorInitializingBoardCallInterop = "Помилка ініціалізації BoardCallInterop: {0}";
    public const string ErrorEnsuringLocalVideoAfterRender = "Помилка забезпечення локального відео після рендерингу: {0}";
    public const string ErrorInitializingBoardCallPage = "Помилка ініціалізації сторінки відеодзвінка: {0}";
    public const string ErrorStartingCall = "Помилка при запуску дзвінка: {0}";
    public const string ErrorHangingUp = "Помилка при завершенні дзвінка: {0}";
    public const string ErrorTogglingCamera = "Помилка при перемиканні камери: {0}";
    public const string ErrorTogglingMic = "Помилка при перемиканні мікрофона: {0}";
    public const string ErrorTogglingScreenShare = "Помилка при перемиканні демонстрації екрана: {0}";
    public const string LocalVideoRestored = "Локальний відеопотік відновлено успішно";
    public const string ErrorRestoringLocalVideo = "Помилка відновлення локального відеопотоку: {0}";
    public const string ErrorEnsuringLocalVideoVisibility = "Помилка забезпечення видимості локального відео: {0}";
    public const string AttachmentsLabel = "Прикріплення:";

    // Board messages
    public const string BoardNotFoundOrAccessDeniedMessage = "Дошку не знайдено або доступ заборонено";
    public const string YouDontHaveAccessToBoard = "У вас немає доступу до цієї дошки";
    public const string FailedToLoadBoard = "Не вдалося завантажити дошку: {0}";
    public const string OnlyBoardOwnersCanAddMembers = "Тільки власники дошки можуть додавати учасників";
    public const string OnlyOwnersAndMembersCanAddTasks = "Тільки власники дошки та учасники можуть додавати завдання";
    public const string OnlyOwnersAndMembersCanManageStates = "Тільки власники дошки та учасники можуть керувати станами";
    public const string OnlyOwnersAndMembersCanManageTags = "Тільки власники дошки та учасники можуть керувати тегами";
    public const string OnlyBoardOwnerCanArchive = "Тільки власник дошки може архівувати дошку";
    public const string BoardArchivedSuccess = "Дошку успішно заархівовано";
    public const string FailedToArchiveBoard = "Не вдалося архівувати дошку: {0}";
    public const string PleaseSelectAtLeastOneUser = "Будь ласка, виберіть щонайменше одного користувача для додавання";

    // Add task modal
    public const string AddNewTask = "Додати нове завдання";
    public const string TaskTitleLabel = "Заголовок завдання";
    public const string EnterTaskTitle = "Введіть заголовок завдання";
    public const string EnterTaskDescription = "Введіть опис завдання (необов'язково)";
    public const string SelectStatePlaceholder = "Виберіть стан";
    public const string SelectAssigneePlaceholder = "Виберіть виконавця (необов'язково)";
    public const string SelectDueDatePlaceholder = "Виберіть термін (необов'язково)";
    public const string PriorityLow = "Низький";
    public const string PriorityNormal = "Нормальний";
    public const string PriorityHigh = "Високий";
    public const string PriorityImmediate = "Негайний";
    public const string NoTagOption = "Без тегу";

    // Add member modal
    public const string AddBoardMembers = "Додати учасників дошки";
    public const string SearchUsersPlaceholder = "Введіть email або ім'я користувача для пошуку...";
    public const string SelectedUsers = "Вибрані користувачі";
    public const string RoleLabelText = "Роль";
    public const string SelectRolePlaceholder = "Оберіть роль";
    public const string MemberLabelText = "Учасник";
    public const string OwnerLabelText = "Власник";

    // Comments
    public const string SearchCommentsPlaceholder = "Пошук коментарів...";
    public const string LoadingComments = "Завантаження коментарів...";
    public const string Edit = "Редагувати";
    public const string Delete = "Видалити";
    public const string EditYourCommentPlaceholder = "Редагуйте свій коментар...";
    public const string Save = "Зберегти";
    public const string LoadingMoreComments = "Завантаження ще коментарів...";
    public const string LoadMoreComments = "Завантажити ще коментарів";
    public const string NoCommentsFound = "Коментарі не знайдено";
    public const string NoCommentsYet = "Ще немає коментарів. Будьте першим, хто прокоментує це завдання";
    public const string AddACommentPlaceholder = "Додати коментар...";
    public const string Attach = "Прикріпити";
    public const string RemoveAll = "Видалити всі";
    public const string Clear = "Очистити";
    public const string CommentButton = "Коментувати";
    public const string CtrlEnterToPost = "Ctrl+Enter для відправки";

    // AI Assistant
    public const string Ask = "Запитати";
    public const string AskPlaceholder = "Запитайте про TaskPilot...";
    public const string AiResponseTitle = "Відповідь AI";

    // Logout component
    public const string SignOut = "Вийти з акаунта";
    public const string SignOutDescription = "Це виведе вас з облікового запису та перенаправить на сторінку входу.";
    
    // Common actions
    public const string Cancel = "Скасувати";
    public const string Yes = "Так";
    public const string No = "Ні";
    public const string SaveChanges = "Зберегти зміни";
    public const string Close = "Закрити";

    // Task modal
    public const string EditTask = "Редагувати завдання";
    public const string DeleteTask = "Видалити завдання";
    public const string ArchiveTask = "Архівувати завдання";
    public const string ConfirmDeleteTask = "Ви впевнені, що хочете видалити це завдання?";
    public const string ConfirmArchiveTask = "Ви впевнені, що хочете архівувати це завдання?";

    // Validation / errors
    public const string BoardNameRequired = "Назва дошки обов'якова";
    public const string PleaseCheckFormAndTryAgain = "Будь ласка, перевірте форму та спробуйте ще раз";
    
    // Descriptions / templates
    public const string SignInWithMicrosoftDescription = "Увійдіть через Microsoft для доступу до ваших дощок та завдань.";
    public const string WelcomeLoggedInAs = "Ви успішно увійшли як {0}";
    // Profile page / edit
    public const string ProfilePageTitle = "Профіль";
    public const string ManageYourProfile = "Керування налаштуваннями та перевагами профілю.";
    public const string EditProfile = "Редагувати профіль";
    public const string BasicInformation = "Основна інформація";
    public const string AccountDetails = "Дані акаунту";
    public const string EmailAddress = "Електронна адреса";
    public const string ToChangeRoleContactAdmin = "Щоб змінити роль, зверніться до адміністратора.";
    public const string UserInfoProvidedByEntra = "Інформація про користувача надається Microsoft Entra ID.";
    public const string LastUpdatedLabel = "Останнє оновлення";
    public const string RemoveAvatar = "Видалити аватар";
    // States / Tags
    public const string AddNewState = "Додати новий стан";
    public const string StateNameLabel = "Назва стану";
    public const string EnterStateNamePlaceholder = "Введіть назву стану (напр., To Do, In Progress)";
    public const string DefaultStatesDescription = "Стандартні стани: To Do, In Progress, Review, Done";

    public const string AddTag = "Додати тег";
    public const string TagNameLabel = "Назва тегу";
    public const string EnterTagNamePlaceholder = "Введіть назву тегу";
    public const string TagColorLabel = "Колір";
    // Archive / Delete / Create board
    public const string ArchiveBoardTitle = "Архівувати дошку";
    public const string ArchiveBoardConfirm = "Ви впевнені, що хочете архівувати цю дошку?";
    public const string QuickActions = "Швидкі дії";

    // Organization Management
    public const string OrganizationMembers = "Учасники організації";
    public const string AddGuest = "Додати гостя";
    public const string RequestManagerRole = "Запросити роль менеджера";
    public const string AddGuestToOrganization = "Додати гостя до організації";
    public const string SearchUserByEmail = "Електронна пошта користувача";
    public const string EnterUserEmail = "Введіть електронну пошту користувача";
    public const string PleaseSelectUser = "Будь ласка, виберіть користувача";
    public const string EmailRequired = "Електронна пошта обов'язкова";
    public const string InvalidEmailFormat = "Невірний формат електронної пошти";
    public const string PrimaryOrganization = "Основна організація";
    public const string Add = "Додати";
    public const string Invited = "Запрошений";
    public const string JoinedAt = "Приєднався";
    public const string Actions = "Дії";
    public const string PromoteToManager = "Підвищити до менеджера";
    public const string DemoteToMember = "Понизити до учасника";
    public const string ConfirmDemoteManager = "Ви впевнені, що хочете понизити цього менеджера до учасника?";
    public const string GuestAddedSuccessfully = "Гість успішно доданий до організації";
    public const string MemberPromotedSuccessfully = "Учасник успішно підвищений до менеджера";
    public const string MemberDemotedSuccessfully = "Менеджер успішно понижений до учасника";

    // Manager Requests
    public const string ManagerRequestInfo = "Ваш запит буде розглянуто адміністраторами системи. Будь ласка, надайте обґрунтування вашого запиту.";
    public const string RequestMessage = "Повідомлення запиту";
    public const string ExplainWhyYouWantToBeManager = "Поясніть, чому ви хочете стати менеджером цієї організації...";
    public const string MessageRequired = "Повідомлення обов'язкове";
    public const string MessageTooLong = "Повідомлення занадто довге (максимум 1000 символів)";
    public const string Send = "Надіслати";
    public const string ManagerRequestSentSuccessfully = "Запит надіслано! Адміністратори розглянуть його найближчим часом.";

    // Admin - Manager Requests
    public const string ManagerRequests = "Запити на роль менеджера";
    public const string ReviewPendingRequests = "Розгляд очікуючих запитів";
    public const string NoPendingRequests = "Немає очікуючих запитів";
    public const string User = "Користувач";
    public const string Organization = "Організація";
    public const string CreatedAt = "Створено";
    public const string Approve = "Підтвердити";
    public const string Reject = "Відхилити";
    public const string ConfirmApproveRequest = "Ви впевнені, що хочете підтвердити цей запит?";
    public const string RequestApprovedSuccessfully = "Запит успішно підтверджено";
    public const string RequestRejectedSuccessfully = "Запит успішно відхилено";

    // Reject Request Modal
    public const string RejectManagerRequest = "Відхилити запит на роль менеджера";
    public const string RejectingRequestFrom = "Відхилення запиту від";
    public const string RejectionNotes = "Примітки про відхилення";
    public const string OptionalRejectionReason = "Необов'язково: вкажіть причину відхилення...";
    public const string NoRequestSelected = "Запит не вибрано";

    // Navigation menu
    public const string Organizations = "Організації";
    public const string Admin = "Адміністрування";
    public const string ManagerRequestsMenu = "Запити менеджерів";
}
