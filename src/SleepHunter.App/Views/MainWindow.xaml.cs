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
using SleepHunter.ViewModels.Editing;
using SleepHunter.ViewModels.Presentation;
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
        private readonly IconManager iconManager;
        private readonly SkillMetadataManager skillMetadata;
        private readonly SpellMetadataManager spellMetadata;
        private readonly StaffMetadataManager staffMetadata;
        private readonly HotkeyAssignmentService hotkeyAssignments;
        private readonly WindowHotkeyRegistrationService
            hotkeyRegistration;
        private readonly IClientMacroConfigurationMapper
            macroConfigurationMapper;
        private readonly ClientMacroConfigurationRegistry
            macroConfigurations;
        private readonly IClientProcessScanner processScanner;
        private readonly ClientLayoutManager clientLayouts;
        private readonly ColorThemeManager colorThemes;
        private readonly UserSettingsManager settingsManager;
        private readonly ClientSessionRegistry clientSessions;
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

        private readonly ClientDiscoveryCoordinator clientDiscovery;
        private ClientMacroConfiguration selectedMacro;

        public MainWindow(
            ILogger logger,
            IReleaseService releaseService,
            WindowHotkeyRegistry hotkeys,
            IconManager iconManager,
            SkillMetadataManager skillMetadata,
            SpellMetadataManager spellMetadata,
            StaffMetadataManager staffMetadata,
            IMacroConfigurationReader macroConfigurationReader,
            IMacroConfigurationWriter macroConfigurationWriter,
            IClientMacroConfigurationMapper macroConfigurationMapper,
            ClientMacroConfigurationRegistry macroConfigurations,
            ClientSessionRegistry clientSessions,
            IClientProcessScanner processScanner,
            ClientLayoutManager clientLayouts,
            ColorThemeManager colorThemes,
            UserSettingsManager settingsManager,
            IRuntimeAutomationSetupFactory runtimeSetupFactory,
            IClientLaunchService clientLaunchService)
        {
            this.logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            this.releaseService = releaseService ??
                throw new ArgumentNullException(
                    nameof(releaseService));
            ArgumentNullException.ThrowIfNull(hotkeys);
            this.iconManager = iconManager ??
                throw new ArgumentNullException(
                    nameof(iconManager));
            this.skillMetadata = skillMetadata ??
                throw new ArgumentNullException(
                    nameof(skillMetadata));
            this.spellMetadata = spellMetadata ??
                throw new ArgumentNullException(
                    nameof(spellMetadata));
            this.staffMetadata = staffMetadata ??
                throw new ArgumentNullException(
                    nameof(staffMetadata));
            ArgumentNullException.ThrowIfNull(
                macroConfigurationReader);
            ArgumentNullException.ThrowIfNull(
                macroConfigurationWriter);
            this.macroConfigurationMapper =
                macroConfigurationMapper ??
                throw new ArgumentNullException(
                    nameof(macroConfigurationMapper));
            this.macroConfigurations =
                macroConfigurations ??
                throw new ArgumentNullException(
                    nameof(macroConfigurations));
            this.clientSessions = clientSessions ??
                throw new ArgumentNullException(
                    nameof(clientSessions));
            this.processScanner = processScanner ??
                throw new ArgumentNullException(
                    nameof(processScanner));
            this.clientLayouts = clientLayouts ??
                throw new ArgumentNullException(
                    nameof(clientLayouts));
            this.colorThemes = colorThemes ??
                throw new ArgumentNullException(
                    nameof(colorThemes));
            this.settingsManager = settingsManager ??
                throw new ArgumentNullException(
                    nameof(settingsManager));
            ArgumentNullException.ThrowIfNull(
                runtimeSetupFactory);
            ArgumentNullException.ThrowIfNull(
                clientLaunchService);
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
                    skillMetadata.Skills,
                    spellMetadata.Spells));
            hotkeyRegistration =
                new WindowHotkeyRegistrationService(
                    hotkeys,
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
                    clientLaunchService,
                    new WpfClientLaunchInteraction(this),
                    () => settingsManager
                        .Settings,
                    () => clientLayouts.Layout,
                    logger);
            clientList = new ClientListViewModel(
                (session, runtime) =>
                    new ClientListItemViewModel(
                        session,
                        macroConfigurations.GetOrCreate(session),
                        runtime,
                        macroConfigurationMapper,
                        runtimeSetupFactory,
                        () => settingsManager.Settings,
                        uiDispatcher,
                        iconManager,
                        skillMetadata,
                        spellMetadata),
                macroPersistence,
                new WpfMacroConfigurationInteraction(this),
                logger,
                clientLaunch);
            clientDiscovery = new ClientDiscoveryCoordinator(
                processScanner.ScanForProcesses,
                ReconcileDetectedProcesses,
                () => settingsManager.Settings
                    .ProcessUpdateInterval,
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

            LoadSkills();
            LoadSpells();
            LoadStaves();
            CalculateLines();

            clientList.MacroPersistence.IsSpellQueueVisible = false;
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
                clientDiscovery.Dispose();
                clientList.Dispose();
            }

            windowSource?.Dispose();

            isDisposed = true;
        }

        private void InitializeLogger()
        {
            if (!settingsManager.Settings.LoggingEnabled)
                return;

            var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            var logFile = $"session-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";

            var logFilePath = Path.Combine(logsDirectory, logFile);
            logger.AddFileTransport(logFilePath);

            logger.LogInfo("Logging initialized");
        }

        private void InitializeHotkeyHook()
        {
            if (windowSource is not null)
                return;

            var helper = new WindowInteropHelper(this);
            windowSource = HwndSource.FromHwnd(helper.Handle);

            windowSource?.AddHook(WindowMessageHook);
            logger.LogInfo("Hotkey hook initialized");
        }

        private void Window_SourceInitialized(
            object sender,
            EventArgs e)
        {
            InitializeHotkeyHook();
            StartClientPolling();
        }

        private void InitializeViews()
        {
            clientSessions.SessionAdded += OnClientSessionAdded;
            clientSessions.SessionRemoved += OnClientSessionRemoved;
            clientList.ClientLoginStateChanged +=
                OnClientLoginStateChanged;

            spellMetadata.SpellAdded += OnSpellManagerUpdated;
            spellMetadata.SpellChanged += OnSpellManagerUpdated;
            spellMetadata.SpellRemoved += OnSpellManagerUpdated;
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
                spell.IsUndefined = !spellMetadata.ContainsSpell(spell.Name);
        }

        private async void OnClientSessionAdded(
            object sender,
            ClientSessionEventArgs e)
        {
            var session = e.Session;
            logger.LogInfo(
                $"Game client process detected with pid: " +
                $"{session.Process.ProcessId}");

            await runtimeClients.AttachAsync(
                new ClientRuntimeDescriptor(
                    new ClientIdentity(
                        $"process:{session.Process.ProcessId}"),
                    session.Process.ProcessId,
                    session.Process.WindowHandle),
                 settingsManager.Settings.ClientUpdateInterval);

            UpdateClientList();
        }

        private async void OnClientSessionRemoved(
            object sender,
            ClientSessionEventArgs e)
        {
            var session = e.Session;
            logger.LogInfo(
                $"Game client process removed with pid: " +
                $"{session.Process.ProcessId}");

            var item = clientList.FindByProcessId(
                session.Process.ProcessId);
            if (item is not null)
                await OnClientLoggedOutAsync(item);

            UpdateClientList();

            if (selectedMacro?.Client == session)
                SelectNextAvailableClient();

            macroConfigurations.Remove(
                session.Process.ProcessId);
            await runtimeClients.DetachAsync(
                session.Process.ProcessId);
        }

        private async void OnClientLoginStateChanged(
            object sender,
            ClientLoginStateChangedEventArgs e)
        {
            await Dispatcher.InvokeAsync(static () => { });

            if (e.IsLoggedIn)
                await OnClientLoggedInAsync(e.Client);
            else
                await OnClientLoggedOutAsync(e.Client);
        }

        private async Task OnClientLoggedInAsync(
            ClientListItemViewModel client)
        {
            if (client == null ||
                string.IsNullOrWhiteSpace(client.Name))
                return;

            await Dispatcher.InvokeAsync(static () => { });

            var session = client.Session;

            logger.LogInfo(
                $"Character logged in: {client.Name} " +
                $"(pid {session.Process.ProcessId})");

            NativeMethods.SetWindowText(
                session.Process.WindowHandle,
                $"{session.Layout?.WindowTitle ?? session.Process.WindowTitle} - {client.Name}");

            var autosaveEnabled = settingsManager.Settings.SaveMacroStates;
            var configuration =
                macroConfigurations.GetOrCreate(session);

            if (autosaveEnabled)
            {
                logger.LogInfo(
                    $"Auto-loading {configuration.Name} macro configuration...");
                await clientList.MacroPersistence.AutoLoadMacroAsync(
                    configuration);
            }

            // Set default spell queue rotation mode
            if (configuration.SpellQueueRotation ==
                SpellRotationMode.Default)
            {
                configuration.SpellQueueRotation =
                    settingsManager.Settings
                        .SpellRotationMode;
            }
        }

        private async Task OnClientLoggedOutAsync(
            ClientListItemViewModel client)
        {
            if (client == null ||
                string.IsNullOrWhiteSpace(client.Name))
                return;

            await Dispatcher.InvokeAsync(static () => { });

            var session = client.Session;

            logger.LogInfo(
                $"Character logged out: {client.Name} " +
                $"(pid {session.Process.ProcessId})");

            NativeMethods.SetWindowText(
                session.Process.WindowHandle,
                session.Layout?.WindowTitle ??
                session.Process.WindowTitle);

            var autosaveEnabled = settingsManager.Settings.SaveMacroStates;
            var configuration =
                macroConfigurations.GetOrCreate(session);

            if (autosaveEnabled)
            {
                logger.LogInfo(
                    $"Auto-saving {configuration.Name} macro configuration...");
                await clientList.MacroPersistence.AutoSaveMacroAsync(
                    configuration);
            }

            if (session.HasHotkey)
            {
                hotkeyRegistration.Unregister(
                    session.Hotkey);
            }

            session.Hotkey = null;

            configuration.ClearSkills();
            configuration.ClearSpellQueue();
            configuration.ClearFlowerQueue();

            UpdateUIForSelectedClient(client.Name);
        }

        private void UpdateUIForSelectedClient(string lastSelectedName = "")
        {
            if (selectedMacro != null && selectedMacro.Name == lastSelectedName)
                SelectNextAvailableClient();

            if (!clientList.HasLoggedInClients)
                clientList.MacroPersistence.IsSpellQueueVisible =
                    false;
        }

        private void SelectNextAvailableClient()
        {
            if (!clientList.HasLoggedInClients)
            {
                clientListBox.SelectedItem = null;
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
                    clientLayouts.LoadFromFile(
                        layoutFile);
                    logger.LogInfo(
                        "Client layout successfully loaded");

                    var layout =
                        clientLayouts.Layout;
                    clientList.ClientLaunch.IsLayoutAvailable =
                        true;
                    if (!string.IsNullOrWhiteSpace(
                            layout.WindowClassName))
                    {
                        processScanner.RegisterWindowClassName(
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
                    colorThemes.LoadFromFile(themesFile);
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
                    settingsManager.LoadFromFile(settingsFile);
                    logger.LogInfo("User settings loaded successfully");

                    if (string.IsNullOrWhiteSpace(settingsManager.Settings.SelectedTheme))
                    {
                        logger.LogWarn("User settings does not have a selected theme, using default theme");
                        settingsManager.Settings.SelectedTheme = colorThemes.DefaultTheme?.Name;
                    }
                    else
                    {
                        var selectedTheme = settingsManager.Settings.SelectedTheme;
                        if (!colorThemes.ContainsTheme(selectedTheme))
                        {
                            logger.LogWarn($"User settings has an invalid theme selected: {selectedTheme}");
                            settingsManager.Settings.SelectedTheme = colorThemes.DefaultTheme?.Name;
                        }
                    }
                }
                else
                {
                    settingsManager.Settings.ResetDefaults();
                    logger.LogInfo("No user settings file was found, using defaults");

                    isFirstRun = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load user settings, resetting to defaults");
                logger.LogException(ex);

                settingsManager.Settings.ResetDefaults();
            }
            finally
            {
                settingsManager.Settings.PropertyChanged += UserSettings_PropertyChanged;

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
                    skillMetadata.LoadFromFile(skillsFile);
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
                    spellMetadata.LoadFromFile(spellsFile);
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
                    staffMetadata.LoadFromFile(stavesFile);
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
            staffMetadata.RecalculateAllStaves();
        }

        private void StartClientPolling()
        {
            clientDiscovery.Start();
        }

        private void ReconcileDetectedProcesses()
        {
            while (processScanner.TryDequeueRemoved(
                       out var deadClient))
            {
                clientSessions.Remove(deadClient.ProcessId);
            }

            while (processScanner.TryDequeueAdded(
                       out var newClient))
            {
                clientSessions.AddDetectedClient(newClient);
            }

            if (clientListBox.SelectedIndex == -1 &&
                clientListBox.Items.Count > 0)
            {
                clientListBox.SelectedIndex = 0;
            }
        }

        private void ApplyTheme()
        {
            var themeName = settingsManager.Settings.SelectedTheme;
            if (string.IsNullOrWhiteSpace(themeName))
            {
                logger.LogWarn("Selected theme is not defined, using default theme");
                themeName = colorThemes.DefaultTheme?.Name;
            }

            if (themeName == null || !colorThemes.ContainsTheme(themeName))
            {
                logger.LogWarn("Theme name is null or invalid, using default theme instead");
                colorThemes.ApplyDefaultTheme();
                return;
            }

            logger.LogInfo($"Applying UI theme: {themeName}");
            colorThemes.ApplyTheme(themeName);
        }

        private async void ActivateHotkey(
            Key key,
            ModifierKeys modifiers)
        {
            var hotkey = hotkeyRegistration.Find(
                key,
                modifiers);

            if (hotkey == null)
                return;

            var client = clientList.FindByHotkey(hotkey);
            if (client is null)
                return;

            logger.LogInfo(
                $"Hotkey {hotkey.Modifiers}+{hotkey.Key} activated " +
                $"for character: {client.Name}");

            if (!client.ToggleMacroCommand.CanExecute(null))
            {
                logger.LogWarn(
                    $"Runtime automation is unavailable for " +
                    $"character: {client.Name} (hotkey)");
                return;
            }

            var wasRunning = client.IsMacroRunning;
            var wasPaused = client.IsMacroPaused;
            await client.ToggleMacroCommand.ExecuteAsync(null);
            if (client.LastAutomationError is { } error)
            {
                logger.LogError(
                    $"Unable to change runtime automation for " +
                    $"character: {client.Name} (hotkey)");
                logger.LogException(error);
                return;
            }

            var action = wasRunning
                ? "Paused"
                : wasPaused
                    ? "Resumed"
                    : "Started";
            logger.LogInfo(
                $"{action} runtime automation for character: " +
                $"{client.Name} (hotkey)");
        }

        private void UpdateListBoxGridWidths()
        {
            var settings = settingsManager.Settings;

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

            var iconSize = settingsManager.Settings.InventoryIconSize;
            inventoryListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void SetSkillGridWidth(int units)
        {
            if (units < 1)
            {
                temuairSkillListBox.MaxWidth = medeniaSkillListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = settingsManager.Settings.SkillIconSize;
            temuairSkillListBox.MaxWidth = medeniaSkillListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void SetWorldSkillGridWidth(int units)
        {
            if (units < 1)
            {
                worldSkillListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = settingsManager.Settings.SkillIconSize;
            worldSkillListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void SetSpellGridWidth(int units)
        {
            if (units < 1)
            {
                temuairSpellListBox.MaxWidth = medeniaSpellListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = settingsManager.Settings.SkillIconSize;
            temuairSpellListBox.MaxWidth = medeniaSpellListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void SetWorldSpellGridWidth(int units)
        {
            if (units < 1)
            {
                worldSpellListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = settingsManager.Settings.SkillIconSize;
            worldSpellListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void ToggleModalOverlay(bool showHide) => modalOverlay.Visibility = showHide ? Visibility.Visible : Visibility.Hidden;

        public void ShowMetadataWindow(int selectedTabIndex = -1)
        {
            if (metadataWindow == null || !metadataWindow.IsLoaded)
            {
                metadataWindow = new MetadataEditorWindow(
                    skillMetadata,
                    spellMetadata,
                    staffMetadata)
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
                settingsWindow =
                    new SettingsWindow(
                        logger,
                        releaseService,
                        settingsManager,
                        colorThemes)
                    {
                        Owner = this
                    };

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

                var updateProgressWindow =
                    new UpdateProgressWindow(releaseService)
                    {
                        Owner = this
                    };
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
                var hotkeyData =
                    unchecked((uint)lParam.ToInt64());
                var key = KeyInterop.KeyFromVirtualKey(
                    (int)(hotkeyData >> 16));
                var modifiers =
                    (ModifierKeys)(hotkeyData & 0xFFFF);

                ActivateHotkey(key, modifiers);
                isHandled = true;
            }

            return nint.Zero;
        }

        private void Window_Shown(object sender, EventArgs e)
        {
            if (isFirstRun)
            {
                logger.LogInfo("Is first launch, prompting user to view the manual...");
                PromptUserToOpenUserManual();
            }

            if (settingsManager.Settings.AutoUpdateEnabled)
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
                await clientDiscovery.DisposeAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    "Unable to stop client polling");
                logger.LogException(ex);
            }

            settingsManager.Settings.PropertyChanged -= UserSettings_PropertyChanged;

            try
            {
                logger.LogInfo("Unregistering all hotkeys...");
                hotkeyRegistration.UnregisterAll();
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
                settingsManager.SaveToFile(settingsFile);
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to save user settings file");
                logger.LogException(ex);
            }

            var configurations =
                macroConfigurations.Configurations.ToList();

            foreach (var configuration in configurations)
            {
                if (clientList.FindByProcessId(
                        configuration.Client.Process.ProcessId) is not
                    { IsLoggedIn: true })
                    continue;

                if (settingsManager.Settings.SaveMacroStates)
                {
                    logger.LogInfo(
                        $"Auto-saving {configuration.Name} macro configuration...");
                    await clientList.MacroPersistence
                        .AutoSaveMacroAsync(
                        configuration,
                        showError: false);
                }
            }

            macroConfigurations.Clear();

            clientSessions.SessionAdded -= OnClientSessionAdded;
            clientSessions.SessionRemoved -= OnClientSessionRemoved;
            clientList.ClientLoginStateChanged -=
                OnClientLoginStateChanged;

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

            var session = item.Session;
            NativeMethods.SetForegroundWindow(
                session.Process.WindowHandle);
            logger.LogInfo(
                $"Setting foreground window for client: " +
                $"{item.Name} (double-click)");
        }

        private void spellQueueListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ListBoxItem listBoxItem)
                return;

            if (listBoxItem.Content is not SpellQueueItemViewModel queueItem)
                return;

            if (selectedMacro == null)
                return;

            var spell = clientList.SelectedClient?
                .Spellbook
                .GetSpell(queueItem.Name);

            if (spell == null)
                return;

            var dialog = new SpellTargetWindow(
                spell,
                queueItem,
                GetLoggedInCharacterNames())
            {
                Owner = this
            };

            logger.LogInfo(
                $"Showing spell '{spell.Name}' target dialog for character: " +
                $"{selectedMacro.Name}");
            var result = dialog.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            selectedMacro.UpdateSpell(
                queueItem,
                dialog.SpellQueueItemViewModel);
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

            if (e.Data.GetData(typeof(SpellQueueItemViewModel)) is not
                    SpellQueueItemViewModel droppedItem ||
                (sender as ListBoxItem)?.DataContext is not
                    SpellQueueItemViewModel target ||
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

            if (listBoxItem.Content is not FlowerQueueItemViewModel queueItem)
                return;

            if (selectedMacro == null)
                return;

            var dialog = new FlowerTargetWindow(
                queueItem,
                GetLoggedInCharacterNames())
            {
                Owner = this
            };

            logger.LogInfo(
                $"Showing flower target dialog for character: " +
                $"{selectedMacro.Name}");
            var result = dialog.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            selectedMacro.UpdateFlower(
                queueItem,
                dialog.FlowerQueueItemViewModel);
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

            if (e.Data.GetData(typeof(FlowerQueueItemViewModel)) is not
                    FlowerQueueItemViewModel droppedItem ||
                (sender as ListBoxItem)?.DataContext is not
                    FlowerQueueItemViewModel target ||
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
                clientList.MacroPersistence.IsSpellQueueVisible =
                    false;
                return;
            }

            var prevSelectedMacro = selectedMacro;
            selectedMacro = item.MacroConfiguration;

            if (selectedMacro == null)
                return;

            if (prevSelectedMacro == null && selectedMacro?.QueuedSpells.Count > 0)
                clientList.MacroPersistence.IsSpellQueueVisible =
                    true;

            if (selectedMacro.QueuedSpells.Count > 0)
            {
                clientList.MacroPersistence.IsSpellQueueVisible =
                    true;
            }
        }

        private async void clientListBox_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            var input = HotkeyInputParser.Parse(
                e.Key,
                e.SystemKey,
                Keyboard.Modifiers);
            if (input.Kind == HotkeyInputKind.Ignore)
                return;

            if (sender is not ListBoxItem
                {
                    Content: ClientListItemViewModel item
                })
                return;

            var session = item.Session;
            logger.LogInfo(
                $"Captured hotkey input {input.Kind} for character: " +
                $"{item.Name}");
            e.Handled = true;
            if (input.Kind == HotkeyInputKind.Clear)
            {
                var clearResult =
                    hotkeyAssignments.Clear(session);
                if (!clearResult.Succeeded)
                {
                    this.ShowMessageBox("Clear Hotkey Error",
                        "There was an error clearing the hotkey, please try again.",
                        "If this continues, try restarting the application.",
                        MessageBoxButton.OK,
                        420, 240);
                    return;
                }

                if (clearResult.Status ==
                    HotkeyAssignmentStatus.Cleared)
                {
                    await PersistHotkeyAssignmentsAsync(
                        [session]);
                }

                return;
            }

            var sessions =
                clientSessions.Sessions.ToArray();
            var affectedSessions = sessions
                .Where(candidate =>
                    ReferenceEquals(candidate, session) ||
                    SameHotkey(
                        candidate.Hotkey,
                        input.Hotkey))
                .ToArray();
            var assignmentResult = hotkeyAssignments.Assign(
                session,
                input.Hotkey,
                sessions);
            if (!assignmentResult.Succeeded)
            {
                this.ShowMessageBox("Set Hotkey Error",
                   "There was an error setting the hotkey, please try again.",
                   "If this continues, try restarting the application.",
                   MessageBoxButton.OK,
                   420, 240);
                return;
            }

            if (assignmentResult.Status ==
                HotkeyAssignmentStatus.Assigned)
            {
                await PersistHotkeyAssignmentsAsync(
                    affectedSessions);
            }
        }

        private async Task PersistHotkeyAssignmentsAsync(
            ClientSession[] sessions)
        {
            if (!settingsManager.Settings
                    .SaveMacroStates)
            {
                return;
            }

            foreach (var session in sessions.Distinct())
            {
                var configuration =
                    macroConfigurations.GetOrCreate(session);
                if (string.IsNullOrWhiteSpace(
                        configuration.Name))
                    continue;

                await clientList.MacroPersistence
                    .AutoSaveMacroAsync(
                        configuration);
            }
        }

        private static bool SameHotkey(
            Hotkey left,
            Hotkey right) =>
            left is not null &&
            right is not null &&
            left.Key == right.Key &&
            left.Modifiers == right.Modifiers;

        private void skillListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ListBoxItem item)
                return;

            if (item.Content is not SkillViewModel skill)
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

            if (item.Content is not SpellViewModel spell)
                return;

            if (clientListBox.SelectedItem is not
                ClientListItemViewModel selectedClient)
                return;

            if (spell.IsEmpty || string.IsNullOrWhiteSpace(spell.Name))
                return;

            if (spell.ArgumentType ==
                SpellArgumentType.TextInput)
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

            var spellTargetWindow = new SpellTargetWindow(
                spell,
                GetLoggedInCharacterNames())
            {
                Owner = this
            };

            logger.LogInfo(
                $"Showing spell '{spell.Name}' target dialog " +
                $"for character: {selectedClient.Name}");
            var result = spellTargetWindow.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            var queueItem = spellTargetWindow.SpellQueueItemViewModel;

            var isAlreadyQueued = selectedMacro.IsSpellInQueue(queueItem.Name);

            if (isAlreadyQueued && settingsManager.Settings.WarnOnDuplicateSpells)
            {
                logger.LogInfo(
                    $"Spell '{spell.Name}' is already queued for " +
                    $"character {selectedClient.Name}, asking user to override");

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

            logger.LogInfo(
                $"Spell '{spell.Name}' added to spell queue for " +
                $"character: {selectedClient.Name}");
        }

        private void addFlowerTargetButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            var flowerTargetDialog = new FlowerTargetWindow(
                GetLoggedInCharacterNames())
            {
                Owner = this
            };

            logger.LogInfo(
                $"Showing flower target dialog for character: " +
                $"{selectedMacro.Name}");
            var result = flowerTargetDialog.ShowDialog();
            if (!result.HasValue || !result.Value)
                return;

            var queueItem = flowerTargetDialog.FlowerQueueItemViewModel;
            queueItem.LastUsedTimestamp = DateTime.Now;

            selectedMacro.AddToFlowerQueue(queueItem);

            logger.LogInfo(
                $"Added '{queueItem.Target}' to flower queue for " +
                $"character: {selectedMacro.Name}");
        }

        private void UserSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not UserSettings settings)
                return;

            logger.LogInfo($"User setting property changed: {e.PropertyName}");

            if (string.Equals(nameof(settings.SelectedTheme), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                ApplyTheme();

            if (string.Equals(nameof(settings.ClientSortOrder), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                UpdateClientList();

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
                UpdateClientList();
        }

        private async void UpdateClientList()
        {
            await Dispatcher.InvokeAsync(static () => { });

            if (isDisposed || isShutdownInProgress)
                return;

            var settings = settingsManager.Settings;
            var showAll = settings.ShowAllProcesses;
            var sortOrder = settings.ClientSortOrder;

            logger.LogInfo($"Updating the client list (showAll = {showAll}, sortOrder = {sortOrder})");

            clientList.Refresh(
                clientSessions.Sessions,
                processId =>
                    runtimeClients.TryFind(
                        processId,
                        out var runtime)
                        ? runtime
                        : null,
                sortOrder,
                showAll);
        }

        private string[] GetLoggedInCharacterNames() =>
            clientList.LoggedInClients
                .Select(client => client.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();

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
