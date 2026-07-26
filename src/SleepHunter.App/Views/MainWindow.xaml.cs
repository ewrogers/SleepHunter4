using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using SleepHunter.Controls;
using SleepHunter.Extensions;
using SleepHunter.Interop.Hosting;
using SleepHunter.IO;
using SleepHunter.IO.Process;
using SleepHunter.Macro;
using SleepHunter.Media;
using SleepHunter.Metadata;
using SleepHunter.Models;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Services.Clients;
using SleepHunter.Services.Configuration;
using SleepHunter.Services.Hotkeys;
using SleepHunter.Services.Logging;
using SleepHunter.Services.Releases;
using SleepHunter.Services.Runtime;
using SleepHunter.Settings;
using SleepHunter.ViewModels;
using SleepHunter.Win32;
using Path = System.IO.Path;

namespace SleepHunter.Views
{
    public partial class MainWindow : Window, IDisposable
    {
        private const int WM_HOTKEY = 0x312;

        private static readonly int IconPadding = 14;

        private readonly ILogger logger;
        private readonly IReleaseService releaseService;
        private readonly HotkeyAssignmentService hotkeyAssignments;
        private readonly IPlayerMacroConfigurationMapper
            macroConfigurationMapper;
        private readonly PlayerMacroConfigurationManager
            macroConfigurations;
        private readonly ClientListViewModel clientList;
        private readonly ClientRuntimeRegistry runtimeClients;

        private bool isDisposed;
        private HwndSource windowSource;

        private bool isFirstRun;
        private bool isShutdownInProgress;
        private bool isShutdownPrepared;
        private int recentSettingsTabIndex;
        private MetadataEditorWindow metadataWindow;
        private SettingsWindow settingsWindow;

        private readonly ClientPollingCoordinator clientPolling;
        private PlayerMacroConfiguration selectedMacro;

        public MainWindow()
        {
            logger = App.Current.Services.GetService<ILogger>();
            releaseService = App.Current.Services.GetService<IReleaseService>();
            var macroConfigurationReader =
                App.Current.Services.GetService<
                    IMacroConfigurationReader>();
            var macroConfigurationWriter =
                App.Current.Services.GetService<
                    IMacroConfigurationWriter>();
            macroConfigurationMapper =
                App.Current.Services.GetService<
                    IPlayerMacroConfigurationMapper>();
            macroConfigurations =
                App.Current.Services.GetService<
                    PlayerMacroConfigurationManager>();
            var runtimeSetupFactory =
                App.Current.Services.GetService<
                    IRuntimeAutomationSetupFactory>();
            var uiDispatcher =
                new WpfUiDispatcher(Dispatcher);
            runtimeClients = new ClientRuntimeRegistry(
                new WindowsClientRuntimeFactory(),
                logger,
                uiDispatcher,
                Path.Combine(
                    AppContext.BaseDirectory,
                    ClientLayoutManager.LayoutFile),
                TimeProvider.System,
                () => AbilitySnapshotCatalogFactory.Create(
                    SkillMetadataManager.Instance.Skills,
                    SpellMetadataManager.Instance.Spells));
            var hotkeyRegistration =
                new WindowHotkeyRegistrationService(
                    () => new WindowInteropHelper(this).Handle);
            hotkeyAssignments = new HotkeyAssignmentService(
                hotkeyRegistration,
                logger);
            var macroPersistence =
                new MacroConfigurationPersistenceService(
                    macroConfigurationReader,
                    macroConfigurationWriter,
                    macroConfigurationMapper,
                    hotkeyRegistration,
                    logger,
                    Environment.CurrentDirectory);
            var clientLaunch =
                new ClientLaunchViewModel(
                    App.Current.Services.GetService<
                        IClientLaunchService>(),
                    new WpfClientLaunchInteraction(this),
                    () => UserSettingsManager.Instance
                        .Settings,
                    () => ClientLayoutManager.Instance
                        .Layout,
                    logger);
            clientList = new ClientListViewModel(
                (player, runtime) =>
                    new ClientListItemViewModel(
                        player,
                        macroConfigurations.GetOrCreate(player),
                        runtime,
                        macroConfigurationMapper,
                        runtimeSetupFactory,
                        () => UserSettingsManager.Instance.Settings,
                        uiDispatcher),
                macroPersistence,
                new WpfMacroConfigurationInteraction(this),
                logger,
                clientLaunch);
            clientPolling = new ClientPollingCoordinator(
                () => ProcessManager.Instance.ScanForProcesses(),
                ReconcileDetectedProcesses,
                () => PlayerManager.Instance.UpdateClients(),
                () => UserSettingsManager.Instance.Settings
                    .ProcessUpdateInterval,
                () => UserSettingsManager.Instance.Settings
                    .ClientUpdateInterval,
                uiDispatcher,
                TimeProvider.System,
                logger);
            DataContext = clientList;

            InitializeLogger();
            InitializeComponent();
            InitializeViews();

            LoadThemes();
            LoadSettings();
            ApplyTheme();
            UpdateListBoxGridWidths();

            LoadClientLayout();

            UpdateWindowTitle();
            LoadSkills();
            LoadSpells();
            LoadStaves();
            CalculateLines();

            ToggleInventory(false);
            ToggleSkills(false);
            ToggleSpells(false);
            clientList.MacroPersistence.IsSpellQueueVisible = false;

            StartClientPolling();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                clientPolling.Dispose();
                clientList.Dispose();
            }

            windowSource?.Dispose();

            isDisposed = true;
        }

        private void InitializeLogger()
        {
            if (!UserSettingsManager.Instance.Settings.LoggingEnabled)
                return;

            var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            var logFile = $"session-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";

            var logFilePath = Path.Combine(logsDirectory, logFile);
            logger.AddFileTransport(logFilePath);

            logger.LogInfo("Logging initialized");
        }

        private void InitializeHotkeyHook()
        {
            var helper = new WindowInteropHelper(this);
            windowSource = HwndSource.FromHwnd(helper.Handle);

            windowSource?.AddHook(WindowMessageHook);
            logger.LogInfo("Hotkey hook initialized");
        }

        private void InitializeViews()
        {
            PlayerManager.Instance.PlayerAdded += OnPlayerCollectionAdd;
            PlayerManager.Instance.PlayerRemoved += OnPlayerCollectionRemove;

            PlayerManager.Instance.PlayerPropertyChanged += OnPlayerPropertyChanged;

            SpellMetadataManager.Instance.SpellAdded += OnSpellManagerUpdated;
            SpellMetadataManager.Instance.SpellChanged += OnSpellManagerUpdated;
            SpellMetadataManager.Instance.SpellRemoved += OnSpellManagerUpdated;
        }

        private void RuntimeDetailsPopup_Closed(
            object sender,
            EventArgs e)
        {
            if (clientList.SelectedClient is { } selectedClient)
                selectedClient.IsRuntimeDetailsOpen = false;
        }

        private void RuntimeDetailsButton_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!runtimeDetailsPopup.IsOpen)
                return;

            if (clientList.SelectedClient is { } selectedClient)
                selectedClient.IsRuntimeDetailsOpen = false;

            e.Handled = true;
        }

        private void OnSpellManagerUpdated(object sender, SpellMetadataEventArgs e)
        {
            if (selectedMacro == null)
                return;

            foreach (var spell in selectedMacro.QueuedSpells)
                spell.IsUndefined = !SpellMetadataManager.Instance.ContainsSpell(spell.Name);
        }

        private async void OnPlayerCollectionAdd(object sender, PlayerEventArgs e)
        {
            logger.LogInfo($"Game client process detected with pid: {e.Player.Process.ProcessId}");

            await runtimeClients.AttachAsync(
                new ClientRuntimeDescriptor(
                    new ClientIdentity(
                        $"process:{e.Player.Process.ProcessId}"),
                    e.Player.Process.ProcessId,
                    e.Player.Process.WindowHandle),
                UserSettingsManager.Instance.Settings.ClientUpdateInterval);
            runtimeClients.BindPresentation(e.Player);

            UpdateClientList();
        }

        private async void OnPlayerCollectionRemove(object sender, PlayerEventArgs e)
        {
            logger.LogInfo($"Game client process removed with pid: {e.Player.Process.ProcessId}");

            runtimeClients.UnbindPresentation(
                e.Player.Process.ProcessId);
            await OnPlayerLoggedOutAsync(e.Player);

            UpdateClientList();

            if (selectedMacro != null && selectedMacro.Name == e.Player.Name)
                SelectNextAvailablePlayer();

            macroConfigurations.Remove(
                e.Player.Process.ProcessId);
            await runtimeClients.DetachAsync(e.Player.Process.ProcessId);
        }

        private async void OnPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not Player player)
                return;

            await Dispatcher.InvokeAsync(static () => { });

            if (string.Equals(nameof(player.IsLoggedIn), e.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                if (!player.IsLoggedIn)
                    await OnPlayerLoggedOutAsync(player);
                else
                    await OnPlayerLoggedInAsync(player);
            }

            UpdateClientList();

            var selectedPlayer =
                (clientListBox.SelectedItem as ClientListItemViewModel)
                ?.Player;

            if (player == selectedPlayer)
            {
                var supportsFlowering =
                    selectedPlayer?.Layout?.SupportsFlowering ??
                    false;
                var hasLyliacPlant = selectedPlayer?.HasLyliacPlant ?? false;
                var hasLyliacVineyard = selectedPlayer?.HasLyliacVineyard ?? false;

                ToggleInventory(selectedPlayer != null);
                ToggleSkills(selectedPlayer != null);
                ToggleSpells(selectedPlayer != null);
                ToggleFlower(supportsFlowering, hasLyliacPlant, hasLyliacVineyard);
            }
        }

        private async Task OnPlayerLoggedInAsync(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.Name))
                return;

            await Dispatcher.InvokeAsync(static () => { });

            player.LoginTimestamp ??= DateTime.Now;

            UpdateClientList();

            logger.LogInfo($"Player logged in: {player.Name} (pid {player.Process.ProcessId})");

            if (!string.IsNullOrEmpty(player.Name))
                NativeMethods.SetWindowText(
                    player.Process.WindowHandle,
                    $"{player.Layout?.WindowTitle ?? player.Process.WindowTitle} - {player.Name}");

            var autosaveEnabled = UserSettingsManager.Instance.Settings.SaveMacroStates;
            var configuration =
                macroConfigurations.GetOrCreate(player);

            if (autosaveEnabled)
            {
                logger.LogInfo(
                    $"Auto-loading {configuration.Client.Name} macro configuration...");
                await clientList.MacroPersistence.AutoLoadMacroAsync(
                    configuration);
            }

            UpdateWindowTitle();

            // Set default spell queue rotation mode
            if (configuration.SpellQueueRotation ==
                SpellRotationMode.Default)
            {
                configuration.SpellQueueRotation =
                    UserSettingsManager.Instance.Settings
                        .SpellRotationMode;
            }
        }

        private async Task OnPlayerLoggedOutAsync(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.Name))
                return;

            await Dispatcher.InvokeAsync(static () => { });

            if (player.LoginTimestamp is null)
                return;

            player.LoginTimestamp = null;
            UpdateClientList();

            logger.LogInfo($"Player logged out: {player.Name} (pid {player.Process.ProcessId})");

            NativeMethods.SetWindowText(
                player.Process.WindowHandle,
                player.Layout?.WindowTitle ??
                player.Process.WindowTitle);

            var autosaveEnabled = UserSettingsManager.Instance.Settings.SaveMacroStates;
            var configuration =
                macroConfigurations.GetOrCreate(player);

            if (autosaveEnabled)
            {
                logger.LogInfo(
                    $"Auto-saving {configuration.Client.Name} macro configuration...");
                await clientList.MacroPersistence.AutoSaveMacroAsync(
                    configuration);
            }

            if (player.HasHotkey)
                HotkeyManager.Instance.UnregisterHotkey(windowSource.Handle, player.Hotkey);

            player.Hotkey = null;

            UpdateWindowTitle();

            configuration.ClearSkills();
            configuration.ClearSpellQueue();
            configuration.ClearFlowerQueue();

            UpdateUIForSelectedClient(player.Name);
        }

        private void UpdateUIForSelectedClient(string lastSelectedName = "")
        {
            if (selectedMacro != null && selectedMacro.Name == lastSelectedName)
                SelectNextAvailablePlayer();

            if (!PlayerManager.Instance.LoggedInPlayers.Any())
                clientList.MacroPersistence.IsSpellQueueVisible =
                    false;
        }

        private void SelectNextAvailablePlayer()
        {
            if (!PlayerManager.Instance.LoggedInPlayers.Any())
            {
                clientListBox.SelectedItem = null;
                UpdateWindowTitle();
                clientList.MacroPersistence.IsSpellQueueVisible =
                    false;
            }
        }

        private void LoadClientLayout()
        {
            var layoutFile = Path.Combine(
                Environment.CurrentDirectory,
                ClientLayoutManager.LayoutFile);
            logger.LogInfo(
                $"Attempting to load the client layout from file: {layoutFile}");
            clientList.ClientLaunch.IsLayoutAvailable =
                false;

            try
            {
                if (File.Exists(layoutFile))
                {
                    ClientLayoutManager.Instance.LoadFromFile(
                        layoutFile);
                    logger.LogInfo(
                        "Client layout successfully loaded");

                    var layout =
                        ClientLayoutManager.Instance.Layout;
                    clientList.ClientLaunch.IsLayoutAvailable =
                        true;
                    if (!string.IsNullOrWhiteSpace(
                            layout.WindowClassName))
                    {
                        ProcessManager.Instance
                            .RegisterWindowClassName(
                                layout.WindowClassName);
                        logger.LogInfo(
                            $"Registered window class name: {layout.WindowClassName}");
                    }
                }
                else
                {
                    logger.LogInfo(
                        "No client layout file was found");

                    this.ShowMessageBox(
                        "Missing Client Layout File",
                        "The client layout file was not found.\nUnable to start new clients.",
                        "You should re-install the application.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    "Failed to load the client layout");
                logger.LogException(ex);

                this.ShowMessageBox(
                    "Unable to Load Client Layout",
                    "The client layout file could not be loaded.\nUnable to start new clients.",
                    "You should re-install the application.");
            }
        }

        private void LoadThemes()
        {
            var themesFile = ColorThemeManager.ThemesFile;
            logger.LogInfo($"Attempting to load themes from file: {themesFile}");

            try
            {
                if (File.Exists(themesFile))
                {
                    ColorThemeManager.Instance.LoadFromFile(themesFile);
                    logger.LogInfo("Themes loaded successfully");
                }
                else
                {
                    logger.LogInfo("No themes file was found, using default theme");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load themes, resetting to default theme");
                logger.LogException(ex);
            }
        }

        private void LoadSettings()
        {
            var settingsFile = UserSettingsManager.SettingsFile;
            logger.LogInfo($"Attempting to user settings from file: {settingsFile}");

            try
            {
                if (File.Exists(settingsFile))
                {
                    UserSettingsManager.Instance.LoadFromFile(settingsFile);
                    logger.LogInfo("User settings loaded successfully");

                    if (string.IsNullOrWhiteSpace(UserSettingsManager.Instance.Settings.SelectedTheme))
                    {
                        logger.LogWarn("User settings does not have a selected theme, using default theme");
                        UserSettingsManager.Instance.Settings.SelectedTheme = ColorThemeManager.Instance.DefaultTheme?.Name;
                    }
                    else
                    {
                        var selectedTheme = UserSettingsManager.Instance.Settings.SelectedTheme;
                        if (!ColorThemeManager.Instance.ContainsTheme(selectedTheme))
                        {
                            logger.LogWarn($"User settings has an invalid theme selected: {selectedTheme}");
                            UserSettingsManager.Instance.Settings.SelectedTheme = ColorThemeManager.Instance.DefaultTheme?.Name;
                        }
                    }
                }
                else
                {
                    UserSettingsManager.Instance.Settings.ResetDefaults();
                    logger.LogInfo("No user settings file was found, using defaults");

                    isFirstRun = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load user settings, resetting to defaults");
                logger.LogException(ex);

                UserSettingsManager.Instance.Settings.ResetDefaults();
            }
            finally
            {
                UserSettingsManager.Instance.Settings.PropertyChanged += UserSettings_PropertyChanged;

                PlayerManager.Instance.SortOrder = UserSettingsManager.Instance.Settings.ClientSortOrder;
                PlayerManager.Instance.ShowAllClients = UserSettingsManager.Instance.Settings.ShowAllProcesses;
                UpdateClientList();
            }
        }

        private void LoadSkills()
        {
            var skillsFile = SkillMetadataManager.SkillMetadataFile;
            logger.LogInfo($"Attempting to skills metadata from file: {skillsFile}");

            try
            {
                if (File.Exists(skillsFile))
                {
                    SkillMetadataManager.Instance.LoadFromFile(skillsFile);
                    logger.LogInfo("Skill metadata loaded successfully");
                }
                else
                {
                    logger.LogWarn("No skills metadata file was found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load skills metadata");
                logger.LogException(ex);
            }
        }

        private void LoadSpells()
        {
            var spellsFile = SpellMetadataManager.SpellMetadataFile;
            logger.LogInfo($"Attempting to spells metadata from file: {spellsFile}");

            try
            {
                if (File.Exists(spellsFile))
                {
                    SpellMetadataManager.Instance.LoadFromFile(spellsFile);
                    logger.LogInfo("Spell metadata loaded successfully");
                }
                else
                {
                    logger.LogWarn("No spells metadata file was found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load spells metadata");
                logger.LogException(ex);
            }
        }

        private void LoadStaves()
        {
            var stavesFile = StaffMetadataManager.StaffMetadataFile;
            logger.LogInfo($"Attempting to staves metadata from file: {stavesFile}");

            try
            {
                if (File.Exists(stavesFile))
                {
                    StaffMetadataManager.Instance.LoadFromFile(stavesFile);
                    logger.LogInfo("Staves metadata loaded successfully");
                }
                else
                {
                    logger.LogWarn("No staves metadata file was found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load staves metadata");
                logger.LogException(ex);
            }
        }

        private void CalculateLines()
        {
            logger.LogInfo("Reculating all staff lines");
            StaffMetadataManager.Instance.RecalculateAllStaves();
        }

        private void StartClientPolling()
        {
            clientPolling.Start();
        }

        private void ReconcileDetectedProcesses()
        {
            while (ProcessManager.Instance.DeadClientCount > 0)
            {
                var deadClient =
                    ProcessManager.Instance.DequeueDeadClient();
                if (deadClient is not null)
                {
                    PlayerManager.Instance.RemovePlayer(
                        deadClient.ProcessId);
                }
            }

            while (ProcessManager.Instance.NewClientCount > 0)
            {
                var newClient =
                    ProcessManager.Instance.DequeueNewClient();
                if (newClient is not null)
                    PlayerManager.Instance.AddNewClient(newClient);
            }

            if (clientListBox.SelectedIndex == -1 &&
                clientListBox.Items.Count > 0)
            {
                clientListBox.SelectedIndex = 0;
            }
        }

        private void ApplyTheme()
        {
            var themeName = UserSettingsManager.Instance.Settings.SelectedTheme;
            if (string.IsNullOrWhiteSpace(themeName))
            {
                logger.LogWarn("Selected theme is not defined, using default theme");
                themeName = ColorThemeManager.Instance.DefaultTheme?.Name;
            }

            if (themeName == null || !ColorThemeManager.Instance.ContainsTheme(themeName))
            {
                logger.LogWarn("Theme name is null or invalid, using default theme instead");
                ColorThemeManager.Instance.ApplyDefaultTheme();
                return;
            }

            logger.LogInfo($"Applying UI theme: {themeName}");
            ColorThemeManager.Instance.ApplyTheme(themeName);
        }

        private async void ActivateHotkey(
            Key key,
            ModifierKeys modifiers)
        {
            var hotkey = HotkeyManager.Instance.GetHotkey(key, modifiers);

            if (hotkey == null)
                return;

            var client = clientList.FindByHotkey(hotkey);
            if (client is null)
                return;

            var hotkeyPlayer = client.Player;
            logger.LogInfo($"Hotkey {hotkey.Modifiers}+{hotkey.Key} activated for character: {hotkeyPlayer.Name}");

            if (!client.ToggleMacroCommand.CanExecute(null))
            {
                logger.LogWarn(
                    $"Runtime automation is unavailable for character: {hotkeyPlayer.Name} (hotkey)");
                return;
            }

            var wasRunning = client.IsMacroRunning;
            var wasPaused = client.IsMacroPaused;
            await client.ToggleMacroCommand.ExecuteAsync(null);
            if (client.LastAutomationError is { } error)
            {
                logger.LogError(
                    $"Unable to change runtime automation for character: {hotkeyPlayer.Name} (hotkey)");
                logger.LogException(error);
                return;
            }

            var action = wasRunning
                ? "Paused"
                : wasPaused
                    ? "Resumed"
                    : "Started";
            logger.LogInfo(
                $"{action} runtime automation for character: {hotkeyPlayer.Name} (hotkey)");
        }

        private void UpdateListBoxGridWidths()
        {
            var settings = UserSettingsManager.Instance.Settings;

            SetInventoryGridWidth(settings.InventoryGridWidth);
            SetSkillGridWidth(settings.SkillGridWidth);
            SetWorldSkillGridWidth(settings.WorldSkillGridWidth);
            SetSpellGridWidth(settings.SpellGridWidth);
            SetWorldSpellGridWidth(settings.WorldSpellGridWidth);
        }

        private void SetInventoryGridWidth(int units)
        {
            if (units < 1)
            {
                inventoryListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.InventoryIconSize;
            inventoryListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void SetSkillGridWidth(int units)
        {
            if (units < 1)
            {
                temuairSkillListBox.MaxWidth = medeniaSkillListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.SkillIconSize;
            temuairSkillListBox.MaxWidth = medeniaSkillListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void SetWorldSkillGridWidth(int units)
        {
            if (units < 1)
            {
                worldSkillListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.SkillIconSize;
            worldSkillListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void SetSpellGridWidth(int units)
        {
            if (units < 1)
            {
                temuairSpellListBox.MaxWidth = medeniaSpellListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.SkillIconSize;
            temuairSpellListBox.MaxWidth = medeniaSpellListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void SetWorldSpellGridWidth(int units)
        {
            if (units < 1)
            {
                worldSpellListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.SkillIconSize;
            worldSpellListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void ToggleModalOverlay(bool showHide) => modalOverlay.Visibility = showHide ? Visibility.Visible : Visibility.Hidden;

        public void ShowMetadataWindow(int selectedTabIndex = -1)
        {
            if (metadataWindow == null || !metadataWindow.IsLoaded)
            {
                metadataWindow = new MetadataEditorWindow
                {
                    Owner = this
                };
            }

            if (selectedTabIndex >= 0)
                metadataWindow.SelectedTabIndex = selectedTabIndex;

            logger.LogInfo("Showing metadata editor window");
            metadataWindow.Show();
        }

        public void ShowSettingsWindow(int selectedTabIndex = -1)
        {
            if (settingsWindow == null || !settingsWindow.IsLoaded)
                settingsWindow = new SettingsWindow() { Owner = this };

            if (selectedTabIndex >= 0)
                settingsWindow.SelectedTabIndex = selectedTabIndex;
            else
                settingsWindow.SelectedTabIndex = recentSettingsTabIndex;

            settingsWindow.Closing += (sender, e) =>
            {
                recentSettingsTabIndex = (sender as SettingsWindow).SelectedTabIndex;
            };
            settingsWindow.Closed += (sender, e) =>
            {
                logger.LogInfo($"Settings window has been closed");
            };

            logger.LogInfo($"Showing settings window (tabIndex = {selectedTabIndex})");
            settingsWindow.Show();
        }

        public void DownloadAndInstallUpdate()
        {
            ToggleModalOverlay(true);
            try
            {
                logger.LogInfo("Attempting to download latest update");

                var updateProgressWindow = new UpdateProgressWindow() { Owner = this };
                updateProgressWindow.ShowDialog();

                if (!updateProgressWindow.ShouldInstall)
                {
                    logger.LogInfo("User has cancelled the update");
                    return;
                }

                var downloadPath = updateProgressWindow.DownloadPath;
                var installationPath = Directory.GetCurrentDirectory();

                UpdateUpdater(downloadPath, installationPath);
                RunUpdater(downloadPath, installationPath);
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to download and install update");
                logger.LogException(ex);
            }
            finally
            {
                ToggleModalOverlay(false);
            }
        }

        private void UpdateUpdater(string updateFile, string installationPath)
        {
            if (!File.Exists(updateFile))
            {
                logger.LogError($"Missing update file, unable to update: {updateFile}");
                return;
            }

            try
            {
                using (var archive = ZipFile.OpenRead(updateFile))
                {
                    var entry = archive.GetEntry("Updater.exe");
                    if (entry == null)
                    {
                        logger.LogWarn($"Updater tool was not found in the update file: {updateFile}");
                        return;
                    }

                    var outputFile = Path.Combine(installationPath, entry.Name);
                    entry.ExtractToFile(outputFile, true);

                    logger.LogInfo($"Successfully updated the Updater tool: {outputFile}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to update the Updater tool");
                logger.LogException(ex);
            }
        }

        private void RunUpdater(string updateFile, string installationPath)
        {
            var updaterExecutable = Path.Combine(installationPath, "Updater.exe");
            logger.LogInfo($"Attempting start the updater executable: {updaterExecutable}");

            if (!File.Exists(updaterExecutable))
            {
                logger.LogError("Updater executable was not found");

                this.ShowMessageBox("Missing Updater", "Unable to start auto-updater executable.", "You may need to install the update manually.");
                return;
            }

            logger.LogInfo($"Starting the updater with arguments: {updateFile} {installationPath}");
            Process.Start(updaterExecutable, $"\"{updateFile}\" \"{installationPath}\"");
            Application.Current.Shutdown();
        }

        private nint WindowMessageHook(nint windowHandle, int message, nint wParam, nint lParam, ref bool isHandled)
        {
            if (message == WM_HOTKEY)
            {
                var key = KeyInterop.KeyFromVirtualKey(lParam.ToInt32() >> 16);
                var modifiers = (ModifierKeys)(lParam.ToInt32() & 0xFFFF);

                ActivateHotkey(key, modifiers);
                isHandled = true;
            }

            return nint.Zero;
        }

        private void Window_Shown(object sender, EventArgs e)
        {
            InitializeHotkeyHook();

            if (isFirstRun)
            {
                logger.LogInfo("Is first launch, prompting user to view the manual...");
                PromptUserToOpenUserManual();
            }

            if (UserSettingsManager.Instance.Settings.AutoUpdateEnabled)
                CheckForNewVersion();
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            if (isShutdownPrepared)
                return;

            if (isShutdownInProgress)
            {
                e.Cancel = true;
                return;
            }

            logger.LogInfo("Application is shutting down");
            e.Cancel = true;
            isShutdownInProgress = true;

            try
            {
                await clientPolling.DisposeAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    "Unable to stop client polling");
                logger.LogException(ex);
            }

            UserSettingsManager.Instance.Settings.PropertyChanged -= UserSettings_PropertyChanged;

            try
            {
                logger.LogInfo("Unregistering all hotkeys...");
                HotkeyManager.Instance.UnregisterAllHotkeys(windowSource.Handle);
                logger.LogInfo("Unregistered all hotkeys successfully");
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to unregister all hotkeys");
                logger.LogException(ex);
            }

            try
            {
                var settingsFile = UserSettingsManager.SettingsFile;

                logger.LogInfo($"Saving user settings to file: {settingsFile}");
                UserSettingsManager.Instance.SaveToFile(settingsFile);
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to save user settings file");
                logger.LogException(ex);
            }

            try
            {
                FileArchiveManager.Instance.ClearArchives();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }

            var configurations =
                macroConfigurations.Configurations.ToList();

            foreach (var configuration in configurations)
            {
                if (configuration.Client is not { IsLoggedIn: true })
                    continue;

                if (UserSettingsManager.Instance.Settings.SaveMacroStates)
                {
                    logger.LogInfo(
                        $"Auto-saving {configuration.Client.Name} macro configuration...");
                    await clientList.MacroPersistence
                        .AutoSaveMacroAsync(
                        configuration,
                        showError: false);
                }
            }

            macroConfigurations.Clear();

            PlayerManager.Instance.PlayerAdded -= OnPlayerCollectionAdd;
            PlayerManager.Instance.PlayerRemoved -= OnPlayerCollectionRemove;
            PlayerManager.Instance.PlayerPropertyChanged -= OnPlayerPropertyChanged;

            try
            {
                await runtimeClients.DisposeAsync();
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to stop all runtime clients");
                logger.LogException(ex);
            }

            logger.LogInfo("Application shutdown tasks have completed");
            isShutdownPrepared = true;
            isShutdownInProgress = false;
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            logger.LogInfo("Main window has been closed");
            Dispose();
        }

        private void PromptUserToOpenUserManual()
        {
            var result = this.ShowMessageBox("Welcome to SleepHunter",
                "It appears to be your first time running the application.\nDo you want to open the user manual?\n\n(This is recommended for new users)",
                "This prompt will not be displayed again.",
                MessageBoxButton.YesNo,
                480, 280);

            if (result.HasValue && result.Value)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(App.USER_MANUAL_URL) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    logger.LogInfo("Unable to open the user manual");
                    logger.LogException(ex);
                }
            }
            else
            {
                logger.LogInfo("User declined to view the user manual");
            }
        }

        #region Toolbar Button Click Methods
        private void metadataEditorButton_Click(object sender, RoutedEventArgs e) => ShowMetadataWindow();
        private void settingsButton_Click(object sender, RoutedEventArgs e) => ShowSettingsWindow();
        #endregion

        private void clientListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ListBoxItem listBoxItem)
                return;

            if (listBoxItem.Content is not ClientListItemViewModel item)
                return;

            var player = item.Player;
            NativeMethods.SetForegroundWindow(player.Process.WindowHandle);
            logger.LogInfo($"Setting foreground window for client: {player.Name} (double-click)");
        }

        private void spellQueueListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ListBoxItem listBoxItem)
                return;

            if (listBoxItem.Content is not SpellQueueItem queueItem)
                return;

            if (selectedMacro == null)
                return;

            var player = selectedMacro.Client;
            var spell = player.Spellbook.GetSpell(queueItem.Name);

            if (spell == null)
                return;

            var dialog = new SpellTargetWindow(spell, queueItem)
            {
                Owner = this
            };

            logger.LogInfo($"Showing spell '{spell.Name}' target dialog for character: {player.Name}");
            var result = dialog.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            selectedMacro.UpdateSpell(
                queueItem,
                dialog.SpellQueueItem);
        }

        private void spellQueueListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || sender is not ListBoxItem draggedItem)
                return;

            logger.LogInfo($"Drag spell queue item: {draggedItem}");

            DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
            draggedItem.IsSelected = true;
        }

        private void spellQueueListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Effects != DragDropEffects.Move)
                return;

            if (e.Data.GetData(typeof(SpellQueueItem)) is not
                    SpellQueueItem droppedItem ||
                (sender as ListBoxItem)?.DataContext is not
                    SpellQueueItem target ||
                clientList.SelectedClient?.MacroEditor is not
                { } editor)
            {
                return;
            }

            logger.LogInfo($"Drop spell queue item: {droppedItem} (target = {target})");

            editor.MoveSpell(droppedItem, target);
        }

        private void spellQueueListBox_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            e.UseDefaultCursors = false;

            Mouse.SetCursor(Cursors.Hand);
            e.Handled = true;
        }

        private void flowerQueueListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ListBoxItem listBoxItem)
                return;

            if (listBoxItem.Content is not FlowerQueueItem queueItem)
                return;

            if (selectedMacro == null)
                return;

            var dialog = new FlowerTargetWindow(queueItem)
            {
                Owner = this
            };

            logger.LogInfo($"Showing flower target dialog for character: {selectedMacro.Client.Name}");
            var result = dialog.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            selectedMacro.UpdateFlower(
                queueItem,
                dialog.FlowerQueueItem);
        }

        private void flowerQueueListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || sender is not ListBoxItem draggedItem)
                return;

            logger.LogInfo($"Drag flower queue item: {draggedItem}");

            DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
            draggedItem.IsSelected = true;
        }

        private void flowerQueueListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Effects != DragDropEffects.Move)
                return;

            if (e.Data.GetData(typeof(FlowerQueueItem)) is not
                    FlowerQueueItem droppedItem ||
                (sender as ListBoxItem)?.DataContext is not
                    FlowerQueueItem target ||
                clientList.SelectedClient?.MacroEditor is not
                { } editor)
            {
                return;
            }

            logger.LogInfo($"Drop flower queue item: {droppedItem} (target = {target})");

            editor.MoveFlower(droppedItem, target);
        }

        private void flowerQueueListBox_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            e.UseDefaultCursors = false;

            Mouse.SetCursor(Cursors.Hand);
            e.Handled = true;
        }

        private void clientListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox
                {
                    SelectedItem: ClientListItemViewModel item
                })
            {
                selectedMacro = null;
                UpdateWindowTitle();
                ToggleInventory(false);
                ToggleSkills(false);
                ToggleSpells(false);
                ToggleFlower(false);
                clientList.MacroPersistence.IsSpellQueueVisible =
                    false;
                return;
            }

            var player = item.Player;
            var prevSelectedMacro = selectedMacro;
            selectedMacro = item.MacroConfiguration;

            UpdateWindowTitle();

            if (selectedMacro == null)
                return;

            tabControl.SelectedIndex = Math.Max(0, selectedMacro.Client.SelectedTabIndex);

            if (prevSelectedMacro == null && selectedMacro?.QueuedSpells.Count > 0)
                clientList.MacroPersistence.IsSpellQueueVisible =
                    true;

            var supportsFlowering =
                player.Layout?.SupportsFlowering ?? false;

            ToggleInventory(player.IsLoggedIn);
            ToggleSkills(player.IsLoggedIn);
            ToggleSpells(player.IsLoggedIn);
            ToggleFlower(supportsFlowering, player.HasLyliacPlant, player.HasLyliacVineyard);

            if (selectedMacro.QueuedSpells.Count > 0)
            {
                clientList.MacroPersistence.IsSpellQueueVisible =
                    true;
            }
        }

        private void clientListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.None)
                return;

            if (sender is not ListBoxItem
                {
                    Content: ClientListItemViewModel item
                })
                return;

            var player = item.Player;
            var key = ((e.Key == Key.System) ? e.SystemKey : e.Key);
            var hasControl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || (e.SystemKey == Key.LeftCtrl || e.SystemKey == Key.RightCtrl);
            var hasAlt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) || (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt);
            var hasShift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || (e.SystemKey == Key.LeftShift || e.SystemKey == Key.RightShift);
            var hasWindows = Keyboard.Modifiers.HasFlag(ModifierKeys.Windows);
            var isFunctionKey = Hotkey.IsFunctionKey(key);

            if (key is Key.LeftCtrl or Key.RightCtrl)
                return;

            if (key == Key.LeftAlt || e.Key == Key.RightAlt)
                return;

            if (key == Key.LeftShift || e.Key == Key.RightShift)
                return;

            if (!hasControl && !hasAlt && !hasShift && !isFunctionKey)
            {
                if (e.Key is Key.Delete or Key.Back or Key.Escape)
                {
                    var clearResult =
                        hotkeyAssignments.Clear(player);
                    if (!clearResult.Succeeded)
                    {
                        this.ShowMessageBox("Clear Hotkey Error",
                            "There was an error clearing the hotkey, please try again.",
                            "If this continues, try restarting the application.",
                            MessageBoxButton.OK,
                            420, 240);
                    }

                    e.Handled = true;
                }

                return;
            }

            var modifiers = ModifierKeys.None;

            if (hasControl)
                modifiers |= ModifierKeys.Control;
            if (hasAlt)
                modifiers |= ModifierKeys.Alt;
            if (hasShift)
                modifiers |= ModifierKeys.Shift;
            if (hasWindows)
                modifiers |= ModifierKeys.Windows;

            var hotkey = new Hotkey(modifiers, key);
            var assignmentResult = hotkeyAssignments.Assign(
                player,
                hotkey,
                PlayerManager.Instance.AllClients);
            if (!assignmentResult.Succeeded)
            {
                this.ShowMessageBox("Set Hotkey Error",
                   "There was an error setting the hotkey, please try again.",
                   "If this continues, try restarting the application.",
                   MessageBoxButton.OK,
                   420, 240);
            }
            e.Handled = true;
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not TabControl)
                return;

            if (selectedMacro == null)
                return;

            TabItem newTab = null;

            if (e.AddedItems.Count > 0)
                newTab = e.AddedItems[0] as TabItem;

            if (newTab != null)
                TabSelected(newTab);
        }

        private void TabSelected(TabItem tab)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.Client.SelectedTabIndex = tabControl.Items.IndexOf(tab);

            var supportsFlowering =
                selectedMacro.Client.Layout?.SupportsFlowering ??
                false;
            ToggleFlower(supportsFlowering, selectedMacro.Client.HasLyliacPlant, selectedMacro.Client.HasLyliacVineyard);
        }

        private void skillListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ListBoxItem item)
                return;

            if (item.Content is not Skill skill)
                return;

            if (clientListBox.SelectedItem is not
                ClientListItemViewModel selectedClient)
                return;

            if (skill.IsEmpty || string.IsNullOrWhiteSpace(skill.Name))
                return;

            var configuration = selectedClient.MacroConfiguration;
            if (configuration is null)
                return;

            logger.LogInfo(
                $"Toggling skill '{skill.Name}' for character: {selectedClient.Name}");
            configuration.ToggleSkill(skill.Name);
        }

        private void spellListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ListBoxItem item)
                return;

            if (item.Content is not Spell spell)
                return;

            if (clientListBox.SelectedItem is not
                ClientListItemViewModel selectedClient)
                return;

            var player = selectedClient.Player;
            if (spell.IsEmpty || string.IsNullOrWhiteSpace(spell.Name))
                return;

            if (spell.TargetType == AbilityTargetType.TextInput)
            {
                this.ShowMessageBox("Not Supported",
                   "This spell requires a user text input and cannot be macroed.",
                   "Only spells with no target or a single target can be macroed.",
                   MessageBoxButton.OK,
                   500, 240);
                return;
            }

            if (selectedMacro == null)
                return;

            var spellTargetWindow = new SpellTargetWindow(spell)
            {
                Owner = this
            };

            logger.LogInfo($"Showing spell '{spell.Name}' target dialog for character: {player.Name}");
            var result = spellTargetWindow.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            var queueItem = spellTargetWindow.SpellQueueItem;

            var isAlreadyQueued = selectedMacro.IsSpellInQueue(queueItem.Name);

            if (isAlreadyQueued && UserSettingsManager.Instance.Settings.WarnOnDuplicateSpells)
            {
                logger.LogInfo($"Spell '{spell.Name}' is already queued for character {player.Name}, asking user to override");

                var userOverride = this.ShowMessageBox("Already Queued",
                   string.Format("The spell '{0}' is already queued.\nDo you want to queue it again anyways?", spell.Name),
                   "This warning message can be disabled in the Spell Macro settings.",
                   MessageBoxButton.YesNo,
                   460, 240);

                if (!userOverride.HasValue || !userOverride.Value)
                    return;
            }

            selectedMacro.AddToSpellQueue(queueItem);
            clientList.MacroPersistence.IsSpellQueueVisible = true;

            logger.LogInfo($"Spell '{spell.Name}' added to spell queue for character: {player.Name}");
        }

        private void addFlowerTargetButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            var flowerTargetDialog = new FlowerTargetWindow
            {
                Owner = this
            };

            logger.LogInfo($"Showing flower target dialog for character: {selectedMacro.Client.Name}");
            var result = flowerTargetDialog.ShowDialog();
            if (!result.HasValue || !result.Value)
                return;

            var queueItem = flowerTargetDialog.FlowerQueueItem;
            queueItem.LastUsedTimestamp = DateTime.Now;

            selectedMacro.AddToFlowerQueue(queueItem);

            logger.LogInfo($"Added '{queueItem.Target}' to flower queue for character: {selectedMacro.Client.Name}");
        }

        private void UserSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not UserSettings settings)
                return;

            logger.LogInfo($"User setting property changed: {e.PropertyName}");

            if (string.Equals(nameof(settings.SelectedTheme), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                ApplyTheme();

            if (string.Equals(nameof(settings.ClientSortOrder), e.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                PlayerManager.Instance.SortOrder = settings.ClientSortOrder;
                UpdateClientList();
            }

            if (string.Equals(nameof(settings.InventoryGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetInventoryGridWidth(settings.InventoryGridWidth);

            if (string.Equals(nameof(settings.SkillGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetSkillGridWidth(settings.SkillGridWidth);

            if (string.Equals(nameof(settings.WorldSkillGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetWorldSkillGridWidth(settings.WorldSkillGridWidth);

            if (string.Equals(nameof(settings.SpellGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetSpellGridWidth(settings.SpellGridWidth);

            if (string.Equals(nameof(settings.WorldSpellGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetWorldSpellGridWidth(settings.WorldSpellGridWidth);

            if (string.Equals(nameof(settings.InventoryIconSize), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                UpdateListBoxGridWidths();

            if (string.Equals(nameof(settings.SkillIconSize), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                UpdateListBoxGridWidths();

            // Debug settings

            if (string.Equals(nameof(settings.ShowAllProcesses), e.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                PlayerManager.Instance.ShowAllClients = settings.ShowAllProcesses;
                UpdateClientList();
            }
        }

        private void UpdateWindowTitle()
        {
            if (selectedMacro == null || !selectedMacro.Client.IsLoggedIn)
            {
                Title = "SleepHunter";
                return;
            }

            Title = $"SleepHunter - {selectedMacro.Client.Name}";
        }

        private void ToggleInventory(bool show = true)
        {
            inventoryTab.IsEnabled = show;
            equipmentTab.IsEnabled = show;
        }

        private void ToggleSkills(bool show = true)
        {
            temuairSkillListBox.Visibility = medeniaSkillListBox.Visibility = worldSkillListBox.Visibility = (show ? Visibility.Visible : Visibility.Collapsed);
            skillsTab.IsEnabled = show;

            if (!show)
                skillsTab.TabIndex = -1;
        }

        private void ToggleSpells(bool show = true)
        {
            temuairSpellListBox.Visibility = medeniaSpellListBox.Visibility = worldSpellListBox.Visibility = (show ? Visibility.Visible : Visibility.Collapsed);
            spellsTab.IsEnabled = show;

            if (!show)
                spellsTab.TabIndex = -1;
        }

        private void ToggleFlower(bool show = false, bool hasLyliacPlant = false, bool hasLyliacVineyard = false)
        {
            flowerTab.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            flowerTab.IsEnabled = hasLyliacPlant || hasLyliacVineyard;

            flowerAlternateCharactersCheckBox.IsEnabled = hasLyliacPlant;
            flowerVineyardCheckBox.IsEnabled = hasLyliacVineyard;

            if (!hasLyliacPlant)
                flowerAlternateCharactersCheckBox.IsChecked = false;

            if (!hasLyliacVineyard)
                flowerVineyardCheckBox.IsChecked = false;
        }

        private async void UpdateClientList()
        {
            await Dispatcher.InvokeAsync(static () => { });

            if (isDisposed || isShutdownInProgress)
                return;

            var showAll = PlayerManager.Instance.ShowAllClients;
            var sortOrder = PlayerManager.Instance.SortOrder;

            logger.LogInfo($"Updating the client list (showAll = {showAll}, sortOrder = {sortOrder})");

            clientList.Refresh(
                PlayerManager.Instance.VisiblePlayers,
                processId =>
                    runtimeClients.TryFind(
                        processId,
                        out var runtime)
                        ? runtime
                        : null);
        }

        private async void CheckForNewVersion()
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            logger.LogInfo($"Checking for new version of the application (current = {currentVersion})");

            try
            {
                var latestRelease = await releaseService.GetLatestReleaseVersionAsync();
                logger.LogInfo($"Latest version is {latestRelease.Version}");

                if (!latestRelease.Version.IsNewerThan(currentVersion))
                    return;

                logger.LogInfo("Prompting the user to update");
                var result = this.ShowMessageBox("New Version Available", $"A newer version ({latestRelease.VersionString}) is available.\n\nDo you want to update now?", "You can disable this on startup in Settings->Updates.", MessageBoxButton.YesNo);

                if (!result.HasValue || !result.Value)
                {
                    logger.LogInfo("User has declined the update");
                    return;
                }

                ShowSettingsWindow(SettingsWindow.UpdatesTabIndex);
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to check for latest version");
                logger.LogException(ex);

                this.ShowMessageBox("Check for Updates", $"Unable to check for a newer version:\n{ex.Message}", "You can disable this on startup in Settings->Updates.");
            }
        }

    }
}
