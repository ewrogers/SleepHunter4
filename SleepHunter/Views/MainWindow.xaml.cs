using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

using Path = System.IO.Path;

using SleepHunter.Extensions;
using SleepHunter.IO;
using SleepHunter.IO.Process;
using SleepHunter.Macro;
using SleepHunter.Media;
using SleepHunter.Metadata;
using SleepHunter.Models;
using SleepHunter.Settings;
using SleepHunter.Win32;
using SleepHunter.Services.Logging;
using SleepHunter.Services.Releases;
using System.Reflection;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SleepHunter.Views
{
    public partial class MainWindow : Window, IDisposable
    {
        private const int WM_HOTKEY = 0x312;

        private static readonly int IconPadding = 14;

        private readonly ILogger logger;
        private readonly IReleaseService releaseService;

        private bool isDisposed;
        private HwndSource windowSource;

        private bool isFirstRun;
        private int recentSettingsTabIndex;
        private MetadataEditorWindow metadataWindow;
        private SettingsWindow settingsWindow;

        private BackgroundWorker processScannerWorker;
        private BackgroundWorker clientUpdateWorker;
        private BackgroundWorker flowerUpdateWorker;

        private PlayerMacroState selectedMacro;

        public MainWindow()
        {
            logger = App.Current.Services.GetService<ILogger>();
            releaseService = App.Current.Services.GetService<IReleaseService>();

            InitializeLogger();
            InitializeComponent();
            InitializeViews();
            
            LoadThemes();
            LoadSettings();
            ApplyTheme();
            UpdateSkillSpellGridWidths();

            LoadVersions();

            UpdateWindowTitle();
            UpdateToolbarState();

            LoadSkills();
            LoadSpells();
            LoadStaves();
            CalculateLines();

            ToggleSkills(false);
            ToggleSpells(false);
            ToggleSpellQueue(false);

            RefreshSpellQueue();
            RefreshFlowerQueue();

            StartUpdateTimers();
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
                processScannerWorker?.Dispose();
                clientUpdateWorker?.Dispose();
                flowerUpdateWorker?.Dispose();
            }

            windowSource?.Dispose();

            isDisposed = true;
        }

        private void LaunchClient()
        {
            startNewClientButton.IsEnabled = false;

            var clientPath = UserSettingsManager.Instance.Settings.ClientPath;

            logger.LogInfo($"Attempting to launch client executable: {clientPath}");

            try
            {
                if (!File.Exists(clientPath))
                {
                    logger.LogError("Client executable not found, unable to launch");
                    return;
                }

                var processInformation = StartClientProcess(clientPath);
                
                if (!TryDetectClientVersion(processInformation, out var detectedVersion))
                {
                    logger.LogWarn("Unable to determine client version, using default version");
                    detectedVersion = ClientVersionManager.Instance.DefaultVersion;
                }
                else
                {
                    logger.LogInfo($"Detected client pid {processInformation.ProcessId} version as {detectedVersion.Key}");
                }

                if (detectedVersion != null)
                    PatchClient(processInformation, detectedVersion);
                else
                    logger.LogWarn($"No client version, unable to apply patches to pid {processInformation.ProcessId}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Unable to launch a new client! Path = {clientPath}");
                logger.LogException(ex);

                this.ShowMessageBox("Launch Client Failed",
                   ex.Message, "Check that the executable exists and the version is correct.",
                   MessageBoxButton.OK,
                   440,
                   280);
            }
            finally
            {
                startNewClientButton.IsEnabled = true;
            }
        }

        private ProcessInformation StartClientProcess(string clientPath)
        {
            // Create Process
            var startupInfo = new StartupInfo { Size = Marshal.SizeOf(typeof(StartupInfo)) };

            var processSecurity = new SecurityAttributes();
            var threadSecurity = new SecurityAttributes();

            processSecurity.Size = Marshal.SizeOf(processSecurity);
            threadSecurity.Size = Marshal.SizeOf(threadSecurity);

            logger.LogInfo($"Attempting to create process for executable: {clientPath}");

            bool wasCreated = NativeMethods.CreateProcess(clientPath,
               null,
               ref processSecurity, ref threadSecurity,
               false,
               ProcessCreationFlags.Suspended,
               nint.Zero,
               null,
               ref startupInfo, out var processInformation);

            // Ensure the process was actually created
            if (!wasCreated || processInformation.ProcessId == 0)
            {
                var errorCode = Marshal.GetLastPInvokeError();
                var errorMessage = Marshal.GetLastPInvokeErrorMessage();
                logger.LogError($"Failed to create client process, code = {errorCode}, message = {errorMessage}");

                throw new Win32Exception(errorCode, "Unable to create client process");
            }
            else
            {
                logger.LogInfo($"Created client process successfully with pid {processInformation.ProcessId}");
            }

            return processInformation;
        }

        private static bool TryDetectClientVersion(ProcessInformation process, out ClientVersion detectedVersion)
        {
            detectedVersion = null;

            using var stream = new ProcessMemoryStream(process.ProcessHandle, ProcessAccess.Read, true);
            using var reader = new BinaryReader(stream, Encoding.ASCII);

            foreach (var version in ClientVersionManager.Instance.Versions)
            {
                // Skip with invalid or missing signatures
                if (version.Signature == null || string.IsNullOrWhiteSpace(version.Signature.Value))
                    continue;

                var signatureLength = version.Signature.Value.Length;

                // Read the signature from the process
                stream.Position = version.Signature.Address;
                var readValue = reader.ReadFixedString(signatureLength);

                // If signature matches the expected value, assume this client version
                if (string.Equals(readValue, version.Signature.Value))
                {
                    detectedVersion = version;
                    return true;
                }
            }

            return false;
        }

        private void PatchClient(ProcessInformation process, ClientVersion version)
        {
            var patchMultipleInstances = UserSettingsManager.Instance.Settings.AllowMultipleInstances;
            var patchIntroVideo = UserSettingsManager.Instance.Settings.SkipIntroVideo;
            var patchNoWalls = UserSettingsManager.Instance.Settings.NoWalls;

            var pid = process.ProcessId;
            logger.LogInfo($"Attempting to patch client process {pid}, version = {version.Key}");

            try
            {
                // Patch Process
                using var accessor = new ProcessMemoryAccessor(pid, ProcessAccess.ReadWrite);
                using var patchStream = accessor.GetWriteableStream();
                using var writer = new BinaryWriter(patchStream, Encoding.ASCII, leaveOpen: true);

                if (patchMultipleInstances && version.MultipleInstanceAddress > 0)
                {
                    logger.LogInfo($"Applying multiple instance patch to process {pid} (0x{version.MultipleInstanceAddress:x8})");
                    patchStream.Position = version.MultipleInstanceAddress;
                    writer.Write((byte)0x31);        // XOR
                    writer.Write((byte)0xC0);        // EAX, EAX
                    writer.Write((byte)0x90);        // NOP
                    writer.Write((byte)0x90);        // NOP
                    writer.Write((byte)0x90);        // NOP
                    writer.Write((byte)0x90);        // NOP
                }

                if (patchIntroVideo && version.IntroVideoAddress > 0)
                {
                    logger.LogInfo($"Applying skip intro video patch to process {pid} (0x{version.IntroVideoAddress:x8})");
                    patchStream.Position = version.IntroVideoAddress;
                    writer.Write((byte)0x83);        // CMP
                    writer.Write((byte)0xFA);        // EDX
                    writer.Write((byte)0x00);        // 0
                    writer.Write((byte)0x90);        // NOP
                    writer.Write((byte)0x90);        // NOP
                    writer.Write((byte)0x90);        // NOP
                }

                if (patchNoWalls && version.NoWallAddress > 0)
                {
                    logger.LogInfo($"Applying no walls patch to process {pid} (0x{version.NoWallAddress:x8})");
                    patchStream.Position = version.NoWallAddress;
                    writer.Write((byte)0xEB);        // JMP SHORT
                    writer.Write((byte)0x17);        // +0x17
                    writer.Write((byte)0x90);        // NOP
                }
            }
            finally
            {
                // Resume and close handles.
                NativeMethods.ResumeThread(process.ThreadHandle);
                NativeMethods.CloseHandle(process.ThreadHandle);
                NativeMethods.CloseHandle(process.ProcessHandle);
            }
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

            windowSource.AddHook(WindowMessageHook);
            logger.LogInfo("Hotkey hook initialized");
        }

        private void InitializeViews()
        {
            PlayerManager.Instance.PlayerAdded += OnPlayerCollectionAdd;
            PlayerManager.Instance.PlayerUpdated += OnPlayerCollectionAdd;
            PlayerManager.Instance.PlayerRemoved += OnPlayerCollectionRemove;

            PlayerManager.Instance.PlayerPropertyChanged += OnPlayerPropertyChanged;

            SpellMetadataManager.Instance.SpellAdded += OnSpellManagerUpdated;
            SpellMetadataManager.Instance.SpellChanged += OnSpellManagerUpdated;
            SpellMetadataManager.Instance.SpellRemoved += OnSpellManagerUpdated;
        }

        private void OnSpellManagerUpdated(object sender, SpellMetadataEventArgs e)
        {
            if (selectedMacro == null)
                return;

            foreach (var spell in selectedMacro.QueuedSpells)
                spell.IsUndefined = !SpellMetadataManager.Instance.ContainsSpell(spell.Name);
        }

        private void OnPlayerCollectionAdd(object sender, PlayerEventArgs e)
        {
            logger.LogInfo($"Game client process detected with pid: {e.Player.Process.ProcessId}");

            UpdateToolbarState();
            UpdateClientList();
        }

        private void OnPlayerCollectionRemove(object sender, PlayerEventArgs e)
        {
            logger.LogInfo($"Game client process removed with pid: {e.Player.Process.ProcessId}");

            OnPlayerLoggedOut(e.Player);

            UpdateToolbarState();
            UpdateClientList();

            if (selectedMacro != null && selectedMacro.Name == e.Player.Name)
                SelectNextAvailablePlayer();
        }

        private void OnPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not Player player)
                return;

            Dispatcher.InvokeIfRequired(() =>
            {
                if (string.Equals("IsLoggedIn", e.PropertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!player.IsLoggedIn)
                        OnPlayerLoggedOut(player);
                    else
                        OnPlayerLoggedIn(player);
                }

                clientListBox.Items.Refresh();

                var selectedPlayer = clientListBox.SelectedItem as Player;

                if (player == selectedPlayer)
                {
                    if (selectedPlayer == null)
                    {
                        ToggleSkills(false);
                        ToggleSpells(false);
                        ToggleFlower(false);
                    }
                    else
                    {
                        ToggleSkills(true);
                        ToggleSpells(true);
                        ToggleFlower(selectedPlayer.HasLyliacPlant, selectedPlayer.HasLyliacVineyard);
                    }
                }

            }, DispatcherPriority.DataBind);
        }

        private void OnPlayerLoggedIn(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.Name))
                return;

            if (!player.LoginTimestamp.HasValue)
                player.LoginTimestamp = DateTime.Now;

            UpdateClientList();

            logger.LogInfo($"Player logged in: {player.Name} (pid {player.Process.ProcessId})");

            if (!string.IsNullOrEmpty(player.Name))
                NativeMethods.SetWindowText(player.Process.WindowHandle, $"Darkages - {player.Name}");

            var shouldRecallMacroState = UserSettingsManager.Instance.Settings.SaveMacroStates;
            var macro = MacroManager.Instance.GetMacroState(player);

            if (macro != null)
            {
                macro.StatusChanged += HandleMacroStatusChanged;
                macro.Client.PlayerUpdated += HandleClientUpdateTick;
            }

            var didLoadFromState = false;

            try
            {
                if (shouldRecallMacroState && macro != null)
                {
                    logger.LogInfo($"Attempting to load previous macro state for character: {player.Name}");
                    LoadMacroState(player);

                    if (macro.SpellQueueRotation == SpellRotationMode.Default)
                        macro.SpellQueueRotation = UserSettingsManager.Instance.Settings.SpellRotationMode;

                    didLoadFromState = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load previous macro state for character: {player.Name}");
                logger.LogException(ex);

                Dispatcher.Invoke((Action)delegate
                {
                    this.ShowMessageBox("Load State Error",
                   string.Format("There was an error loading the macro state:\n{0}", ex.Message),
                   string.Format("{0}'s macro state was not preserved.", player.Name),
                   MessageBoxButton.OK, 460, 260);

                }, DispatcherPriority.Normal, null);

            }
            finally
            {
                UpdateWindowTitle();

                // Set default spell queue rotation mode
                if (!didLoadFromState)
                    macro.SpellQueueRotation = UserSettingsManager.Instance.Settings.SpellRotationMode;
            }
        }

        private void OnPlayerLoggedOut(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.Name))
                return;

            player.LoginTimestamp = null;
            UpdateClientList();

            logger.LogInfo($"Player logged out: {player.Name} (pid {player.Process.ProcessId})");

            NativeMethods.SetWindowText(player.Process.WindowHandle, "Darkages");

            var shouldSaveMacroStates = UserSettingsManager.Instance.Settings.SaveMacroStates;
            var macro = MacroManager.Instance.GetMacroState(player);

            try
            {
                if (shouldSaveMacroStates && macro != null)
                {
                    logger.LogInfo($"Attempting to save macro state for character: {player.Name}");
                    SaveMacroState(macro);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to save macro state for character: {player.Name}");
                logger.LogException(ex);

                Dispatcher.Invoke((Action)delegate
                {
                    this.ShowMessageBox("Save State Error",
                       string.Format("There was an error saving the macro state:\n{0}", ex.Message),
                       string.Format("{0}'s macro state may not be preserved upon next login.", player.Name),
                       MessageBoxButton.OK, 460, 260);

                }, DispatcherPriority.Normal, null);
            }
            finally
            {
                Dispatcher.InvokeIfRequired(() =>
                {
                    if (player.HasHotkey)
                        HotkeyManager.Instance.UnregisterHotkey(windowSource.Handle, player.Hotkey);

                    player.Hotkey = null;
                });

                UpdateWindowTitle();
            }

            if (macro != null)
            {
                macro.StatusChanged -= HandleMacroStatusChanged;
                macro.Client.PlayerUpdated -= HandleClientUpdateTick;

                macro.ClearSpellQueue();
                macro.ClearFlowerQueue();
            }

            if (selectedMacro != null && selectedMacro.Name == player.Name)
                SelectNextAvailablePlayer();

            if (!PlayerManager.Instance.LoggedInPlayers.Any())
                ToggleSpellQueue(false);
        }

        private void HandleMacroStatusChanged(object sender, MacroStatusEventArgs e)
        {
            UpdateToolbarState();
        }

        private void HandleClientUpdateTick(object sender, EventArgs e)
        {
            if (selectedMacro == null || sender is not Player player)
                return;

            if (selectedMacro.Client != player)
                return;

            // Refresh the spell queue levels on tick
            foreach (var queuedSpell in selectedMacro.QueuedSpells)
            {
                var spell = player.Spellbook.GetSpell(queuedSpell.Name);
                if (spell is null)
                    continue;

                queuedSpell.MaximumLevel = spell.MaximumLevel;
                queuedSpell.CurrentLevel = spell.CurrentLevel;
                queuedSpell.IsOnCooldown = spell.IsOnCooldown;
            }
        }

        private void SelectNextAvailablePlayer()
        {
            if (!PlayerManager.Instance.LoggedInPlayers.Any())
            {
                clientListBox.SelectedItem = null;
                UpdateToolbarState();
                UpdateWindowTitle();
                ToggleSpellQueue(false);
                return;
            }
        }

        private void RefreshSpellQueue()
        {
            if (!CheckAccess())
            {
                Dispatcher.InvokeIfRequired(RefreshSpellQueue, DispatcherPriority.DataBind);
                return;
            }

            var hasItemsInQueue = selectedMacro != null && selectedMacro.QueuedSpells.Count > 0;

            removeSelectedSpellButton.IsEnabled = hasItemsInQueue;
            removeAllSpellsButton.IsEnabled = hasItemsInQueue;

            spellQueueListBox.ItemsSource = selectedMacro?.QueuedSpells ?? null;
            spellQueueListBox.Items.Refresh();
        }

        private void RefreshFlowerQueue()
        {
            if (!CheckAccess())
            {
                Dispatcher.InvokeIfRequired(RefreshFlowerQueue, DispatcherPriority.DataBind);
                return;
            }

            var hasItemsInQueue = selectedMacro != null && selectedMacro.FlowerQueueCount > 0;

            removeSelectedFlowerTargetButton.IsEnabled = hasItemsInQueue;
            removeAllFlowerTargetsButton.IsEnabled = hasItemsInQueue;

            flowerListBox.ItemsSource = selectedMacro?.FlowerTargets ?? null;
            flowerListBox.Items.Refresh();
        }

        private void LoadVersions()
        {
            var versionsFile = Path.Combine(Environment.CurrentDirectory, ClientVersionManager.VersionsFile);
            logger.LogInfo($"Attempting to load client versions from file: {versionsFile}");

            try
            {
                if (File.Exists(versionsFile))
                {
                    ClientVersionManager.Instance.LoadFromFile(versionsFile);
                    logger.LogInfo("Client versions successfully loaded");

                    // Register all window class names so they can be detected
                    foreach (var version in ClientVersionManager.Instance.Versions)
                    {
                        if (string.IsNullOrWhiteSpace(version.WindowClassName))
                            continue;

                        ProcessManager.Instance.RegisterWindowClassName(version.WindowClassName);
                        logger.LogInfo($"Registered window class name: {version.WindowClassName} (version = {version.Key})");
                    }
                }
                else
                {
                    UpdateToolbarState();
                    logger.LogInfo("No client version file was found");

                    this.ShowMessageBox("Missing Client Versions File", "The client versions file was not found.\nUnable to start new clients.", "You should re-install the application.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load client versions");
                logger.LogException(ex);

                UpdateToolbarState();

                this.ShowMessageBox("Unable to Load Client Versions", "The client versions file could not be loaded.\nUnable to start new clients.", "You should re-install the application.");
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

        private void StartUpdateTimers()
        {
            IconManager.Instance.Context = TaskScheduler.FromCurrentSynchronizationContext();

            StartProcessScanner();
            StartClientUpdate();
            StartFlowerUpdate();
        }

        private void StartProcessScanner()
        {
            processScannerWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };

            processScannerWorker.DoWork += (sender, e) =>
            {
                var delayTime = (TimeSpan)e.Argument;

                if (delayTime > TimeSpan.Zero)
                    Thread.Sleep(delayTime);

                ProcessManager.Instance.ScanForProcesses();
            };

            processScannerWorker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Cancelled)
                    return;

                // Dead Clients
                while (ProcessManager.Instance.DeadClientCount > 0)
                {
                    var deadClient = ProcessManager.Instance.DequeueDeadClient();
                    PlayerManager.Instance.RemovePlayer(deadClient.ProcessId);
                }

                // New Clients
                while (ProcessManager.Instance.NewClientCount > 0)
                {
                    var newClient = ProcessManager.Instance.DequeueNewClient();
                    PlayerManager.Instance.AddNewClient(newClient);
                }

                if (clientListBox.SelectedIndex == -1 && clientListBox.Items.Count > 0)
                    clientListBox.SelectedIndex = 0;

                processScannerWorker.RunWorkerAsync(UserSettingsManager.Instance.Settings.ProcessUpdateInterval);
            };

            // Start immediately!
            processScannerWorker.RunWorkerAsync(TimeSpan.Zero);
            logger.LogInfo("Process scanner background worker has started");
        }

        private void StartClientUpdate()
        {
            clientUpdateWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };

            clientUpdateWorker.DoWork += (sender, e) =>
            {
                var delayTime = (TimeSpan)e.Argument;

                if (delayTime > TimeSpan.Zero)
                    Thread.Sleep(delayTime);

                PlayerManager.Instance.UpdateClients();
            };

            clientUpdateWorker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Cancelled)
                    return;

                clientUpdateWorker.RunWorkerAsync(UserSettingsManager.Instance.Settings.ClientUpdateInterval);
            };

            // Start immediately!
            clientUpdateWorker.RunWorkerAsync(TimeSpan.Zero);
            logger.LogInfo("Client update background worker has started");
        }

        private void StartFlowerUpdate()
        {
            var updateInterval = TimeSpan.FromMilliseconds(16);

            flowerUpdateWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };

            flowerUpdateWorker.DoWork += (sender, e) =>
            {
                var delayTime = (TimeSpan)e.Argument;

                if (delayTime > TimeSpan.Zero)
                    Thread.Sleep(delayTime);

                foreach (var macro in MacroManager.Instance.Macros)
                    if (macro.Status == MacroStatus.Running)
                        macro.TickFlowerTimers(updateInterval);
            };

            flowerUpdateWorker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Cancelled)
                    return;

                flowerUpdateWorker.RunWorkerAsync(updateInterval);
            };

            flowerUpdateWorker.RunWorkerAsync(updateInterval);
            logger.LogInfo($"Flower update background worker has started, interval = {updateInterval.TotalMilliseconds:0}ms");
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

        private void ActivateHotkey(Key key, ModifierKeys modifiers)
        {
            var hotkey = HotkeyManager.Instance.GetHotkey(key, modifiers);

            if (hotkey == null)
                return;

            Player hotkeyPlayer = null;

            foreach (var player in PlayerManager.Instance.LoggedInPlayers)
                if (player.HasHotkey && player.Hotkey.Key == hotkey.Key && player.Hotkey.Modifiers == hotkey.Modifiers)
                {
                    hotkeyPlayer = player;
                    break;
                }

            if (hotkeyPlayer == null)
                return;

            logger.LogInfo($"Hotkey {hotkey.Modifiers}+{hotkey.Key} activated for character: {hotkeyPlayer.Name}");

            var macroState = MacroManager.Instance.GetMacroState(hotkeyPlayer);

            if (macroState == null)
                return;

            if (macroState.Status == MacroStatus.Running)
            {
                macroState.Pause();
                logger.LogInfo($"Paused macro state for character: {hotkeyPlayer.Name} (hotkey)");
            }
            else
            {
                hotkeyPlayer.Update(PlayerFieldFlags.Location);
                macroState.Start();

                logger.LogInfo($"Started macro state for character: {hotkeyPlayer.Name} (hotkey)");
            }
        }

        private void UpdateSkillSpellGridWidths()
        {
            var settings = UserSettingsManager.Instance.Settings;

            SetSkillGridWidth(settings.SkillGridWidth);
            SetWorldSkillGridWidth(settings.WorldSkillGridWidth);
            SetSpellGridWidth(settings.SpellGridWidth);
            SetWorldSpellGridWidth(settings.WorldSpellGridWidth);
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

        private void UpdateUIForMacroStatus(MacroStatus status)
        {
            if (!CheckAccess())
            {
                Dispatcher.InvokeIfRequired(UpdateUIForMacroStatus, status, DispatcherPriority.DataBind);
                return;
            }

            switch (status)
            {
                case MacroStatus.Running:
                    startMacroButton.Tag = "Start Macro";
                    startMacroButton.IsEnabled = false;
                    pauseMacroButton.IsEnabled = true;
                    stopMacroButton.IsEnabled = true;
                    break;

                case MacroStatus.Paused:
                    startMacroButton.Tag = "Resume Macro";
                    startMacroButton.IsEnabled = true;
                    pauseMacroButton.IsEnabled = false;
                    stopMacroButton.IsEnabled = true;
                    break;

                default:
                    startMacroButton.Tag = "Start Macro";
                    startMacroButton.IsEnabled = true;
                    pauseMacroButton.IsEnabled = false;
                    stopMacroButton.IsEnabled = false;
                    break;
            }
        }

        private void ToggleModalOverlay(bool showHide) => modalOverlay.Visibility = showHide ? Visibility.Visible : Visibility.Hidden;

        private void ToggleSpellQueue(bool showQueue)
        {
            if (spellQueueListBox == null)
                return;

            logger.LogInfo($"Toggle spell queue panel: {showQueue}");

            if (showQueue)
            {
                Grid.SetColumnSpan(tabControl, 1);
                spellQueueListBox.Visibility = Visibility.Visible;
            }
            else
            {
                Grid.SetColumnSpan(tabControl, 2);
                spellQueueListBox.Visibility = Visibility.Collapsed;
            }
        }

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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            logger.LogInfo("Application is shutting down");

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

            try
            {
                if (!Directory.Exists("saves"))
                    Directory.CreateDirectory("saves");
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to create saves directory");
                logger.LogException(ex);
                return;
            }

            var shouldSaveStates = UserSettingsManager.Instance.Settings.SaveMacroStates;

            if (shouldSaveStates)
            {
                foreach (var macro in MacroManager.Instance.Macros)
                {
                    try
                    {
                        if (macro.Client == null || !macro.Client.IsLoggedIn)
                            continue;

                        if (!string.IsNullOrEmpty(macro.Client.Name))
                        {
                            macro.Stop();
                            SaveMacroState(macro);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to save macro state for character '{macro.Name}'!");
                        logger.LogException(ex);
                    }
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            logger.LogInfo("Main window has been closed");
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

        private void SaveMacroState(PlayerMacroState macro)
        {
            if (macro == null)
                throw new ArgumentNullException(nameof(macro));

            var filename = Path.Combine("saves", string.Format("{0}.xml", macro.Client.Name.Trim()));
            var state = new SavedMacroState(macro);

            logger.LogInfo($"Saving macro state to file: {filename}");
            state.SaveToFile(filename);
        }

        private void LoadMacroState(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            if (!Directory.Exists("saves"))
                return;

            var filename = Path.Combine("saves", string.Format("{0}.xml", player.Name));

            if (!File.Exists(filename))
                return;

            logger.LogInfo($"Loading macro state from file: {filename}");
            var macroState = SavedMacroState.LoadFromFile(filename);

            if (macroState == null)
                return;

            MacroManager.Instance.ImportMacroState(player, macroState);

            if (File.Exists(filename))
                File.Delete(filename);

            RefreshSpellQueue();
            RefreshFlowerQueue();
        }

        #region Toolbar Button Click Methods
        private void startNewClientButton_Click(object sender, RoutedEventArgs e) => LaunchClient();

        private void startMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null || selectedMacro.Client == null || !selectedMacro.Client.IsLoggedIn)
                return;

            selectedMacro.Client.Update();
            selectedMacro.Start();
            UpdateToolbarState();

            logger.LogInfo($"Started macro state for character: {selectedMacro.Client.Name} (toolbar)");
        }

        private void pauseMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.Pause();
            UpdateToolbarState();

            logger.LogInfo($"Paused macro state for character {selectedMacro.Client.Name} (toolbar)");
        }

        private void stopMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.Stop();
            UpdateToolbarState();

            logger.LogInfo($"Stopped macro state for character {selectedMacro.Client.Name} (toolbar)");
        }

        private void stopAllMacrosButton_Click(object sender, RoutedEventArgs e)
        {
            MacroManager.Instance.StopAll();
            UpdateToolbarState();

            logger.LogInfo("Stopped all macro states (toolbar)");
        }

        private void showSpellQueueButton_Click(object sender, RoutedEventArgs e) => ToggleSpellQueue(true);
        private void hideSpellQueueButton_Click(object sender, RoutedEventArgs e) => ToggleSpellQueue(false);
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

            if (listBoxItem.Content is not Player player)
                return;

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

            dialog.SpellQueueItem.CopyTo(queueItem);
        }

        private void spellQueueListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not ListBoxItem listBoxItem)
                return;

            if (listBoxItem.Content is not SpellQueueItem spell)
                return;

            if (selectedMacro == null)
                return;

            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                logger.LogInfo($"Removing spell '{spell.Name}' from spell queue for character: {selectedMacro.Client.Name}");

                if (selectedMacro.RemoveFromSpellQueue(spell))
                    RefreshSpellQueue();
            }
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

            var droppedItem = e.Data.GetData(typeof(SpellQueueItem)) as SpellQueueItem;
            var target = (sender as ListBoxItem)?.DataContext as SpellQueueItem;

            var removedIndex = spellQueueListBox.Items.IndexOf(droppedItem);
            var targetIndex = spellQueueListBox.Items.IndexOf(target);

            logger.LogInfo($"Drop spell queue item: {droppedItem} (target = {target})");

            if (removedIndex < targetIndex)
            {
                selectedMacro.AddToSpellQueue(droppedItem, targetIndex + 1);
                selectedMacro.RemoveFromSpellQueueAtIndex(removedIndex);
            }
            else
            {
                if (selectedMacro.QueuedSpells.Count + 1 > removedIndex + 1)
                {
                    selectedMacro.AddToSpellQueue(droppedItem, targetIndex);
                    selectedMacro.RemoveFromSpellQueueAtIndex(removedIndex + 1);
                }
            }

            RefreshSpellQueue();
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

            dialog.FlowerQueueItem.CopyTo(queueItem);
        }

        private void flowerQueueListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not ListBoxItem listBoxItem)
                return;

            if (listBoxItem.Content is not FlowerQueueItem flower)
                return;

            if (selectedMacro == null)
                return;

            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                logger.LogInfo($"Removing '{flower.Target}' from flower queue for character: {selectedMacro.Name}");

                if (selectedMacro.RemoveFromFlowerQueue(flower))
                    RefreshFlowerQueue();
            }
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

            var droppedItem = e.Data.GetData(typeof(FlowerQueueItem)) as FlowerQueueItem;
            var target = (sender as ListBoxItem)?.DataContext as FlowerQueueItem;

            var removedIndex = flowerListBox.Items.IndexOf(droppedItem);
            var targetIndex = flowerListBox.Items.IndexOf(target);

            logger.LogInfo($"Drop flower queue item: {droppedItem} (target = {target})");

            if (removedIndex < targetIndex)
            {
                selectedMacro.AddToFlowerQueue(droppedItem, targetIndex + 1);
                selectedMacro.RemoveFromFlowerQueueAtIndex(removedIndex);
            }
            else
            {
                if (selectedMacro.FlowerTargets.Count + 1 > removedIndex + 1)
                {
                    selectedMacro.AddToFlowerQueue(droppedItem, targetIndex);
                    selectedMacro.RemoveFromFlowerQueueAtIndex(removedIndex + 1);
                }
            }

            RefreshFlowerQueue();
        }

        private void flowerQueueListBox_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            e.UseDefaultCursors = false;

            Mouse.SetCursor(Cursors.Hand);
            e.Handled = true;
        }

        private void clientListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox || listBox.SelectedItem is not Player player)
            {
                if (selectedMacro != null)
                    selectedMacro.PropertyChanged -= SelectedMacro_PropertyChanged;

                selectedMacro = null;
                UpdateWindowTitle();
                ToggleSkills(false);
                ToggleSpells(false);
                ToggleFlower();
                UpdateToolbarState();
                return;
            }

            var macroState = MacroManager.Instance.GetMacroState(player);

            UnsubscribeMacroHandlers(selectedMacro);
            var prevSelectedMacro = selectedMacro;
            selectedMacro = macroState;
            SubscribeMacroHandlers(selectedMacro);

            UpdateWindowTitle();
            UpdateToolbarState();

            if (selectedMacro == null)
                return;

            tabControl.SelectedIndex = Math.Max(0, selectedMacro.Client.SelectedTabIndex);

            if (prevSelectedMacro == null && selectedMacro?.QueuedSpells.Count > 0)
                ToggleSpellQueue(true);

            ToggleSkills(player.IsLoggedIn);
            ToggleSpells(player.IsLoggedIn);
            ToggleFlower(player.HasLyliacPlant, player.HasLyliacVineyard);

            if (selectedMacro != null)
            {
                spellQueueRotationComboBox.SelectedValue = selectedMacro.SpellQueueRotation;

                spellQueueListBox.ItemsSource = selectedMacro.QueuedSpells;
                RefreshSpellQueue();

                flowerListBox.ItemsSource = selectedMacro.FlowerTargets;
                RefreshFlowerQueue();

                flowerVineyardCheckBox.IsChecked = selectedMacro.UseLyliacVineyard && player.HasLyliacVineyard;
                flowerAlternateCharactersCheckBox.IsChecked = selectedMacro.FlowerAlternateCharacters && player.HasLyliacPlant;

                foreach (var spell in selectedMacro.QueuedSpells)
                    spell.IsUndefined = !SpellMetadataManager.Instance.ContainsSpell(spell.Name);
            }
        }

        private void SubscribeMacroHandlers(PlayerMacroState state)
        {
            if (state != null)
                return;

            state.PropertyChanged += SelectedMacro_PropertyChanged;

            state.SpellAdded += selectedMacro_SpellQueueChanged;
            state.SpellUpdated += selectedMacro_SpellQueueChanged;
            state.SpellRemoved += selectedMacro_SpellQueueChanged;

            state.FlowerTargetAdded += selectedMacro_FlowerQueueChanged;
            state.FlowerTargetUpdated += selectedMacro_FlowerQueueChanged;
            state.FlowerTargetRemoved += selectedMacro_FlowerQueueChanged;
        }

        private void UnsubscribeMacroHandlers(PlayerMacroState state)
        {
            if (state == null)
                return;

            state.PropertyChanged -= SelectedMacro_PropertyChanged;

            state.SpellAdded -= selectedMacro_SpellQueueChanged;
            state.SpellUpdated -= selectedMacro_SpellQueueChanged;
            state.SpellRemoved -= selectedMacro_SpellQueueChanged;

            state.FlowerTargetAdded -= selectedMacro_FlowerQueueChanged;
            state.FlowerTargetUpdated -= selectedMacro_FlowerQueueChanged;
            state.FlowerTargetRemoved -= selectedMacro_FlowerQueueChanged;
        }

        private void clientListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.None)
                return;

            if (sender is not ListBoxItem listBoxItem || listBoxItem.Content is not Player player)
                return;

            var key = ((e.Key == Key.System) ? e.SystemKey : e.Key);
            var hasControl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || (e.SystemKey == Key.LeftCtrl || e.SystemKey == Key.RightCtrl);
            var hasAlt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) || (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt);
            var hasShift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || (e.SystemKey == Key.LeftShift || e.SystemKey == Key.RightShift);
            var hasWindows = Keyboard.Modifiers.HasFlag(ModifierKeys.Windows);
            var isFunctionKey = Hotkey.IsFunctionKey(key);

            if (key == Key.LeftCtrl || key == Key.RightCtrl)
                return;

            if (key == Key.LeftAlt || e.Key == Key.RightAlt)
                return;

            if (key == Key.LeftShift || e.Key == Key.RightShift)
                return;

            if (!hasControl && !hasAlt && !hasShift && !isFunctionKey)
            {
                if (e.Key == Key.Delete || e.Key == Key.Back || e.Key == Key.Escape)
                {
                    if (player.Hotkey != null)
                    {
                        logger.LogInfo($"Clearing hotkey for character: {player.Name}");
                        HotkeyManager.Instance.UnregisterHotkey(windowSource.Handle, player.Hotkey);
                    }

                    player.Hotkey = null;
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
            var oldHotkey = HotkeyManager.Instance.GetHotkey(hotkey.Key, hotkey.Modifiers);

            if (oldHotkey != null)
            {
                foreach (var p in PlayerManager.Instance.AllClients)
                {
                    if (!p.HasHotkey)
                        continue;

                    if (p.Hotkey.Key == hotkey.Key && p.Hotkey.Modifiers == hotkey.Modifiers)
                        p.Hotkey = null;
                }
            }

            HotkeyManager.Instance.UnregisterHotkey(windowSource.Handle, hotkey);

            if (!HotkeyManager.Instance.RegisterHotkey(windowSource.Handle, hotkey))
            {
                logger.LogError($"Unable to set hotkey {hotkey.Modifiers}+{hotkey.Key} for character: {player.Name}");

                this.ShowMessageBox("Set Hotkey Error",
                   "There was an error setting the hotkey, please try again.",
                   "If this continues, try restarting the application.",
                   MessageBoxButton.OK,
                   420, 240);
            }
            else
            {
                if (player.Hotkey != null)
                    HotkeyManager.Instance.UnregisterHotkey(windowSource.Handle, player.Hotkey);

                player.Hotkey = hotkey;
            }

            e.Handled = true;
        }

        private void selectedMacro_SpellQueueChanged(object sender, SpellQueueItemEventArgs e)
        {
            if (sender is not PlayerMacroState macro)
                return;

            spellQueueListBox.ItemsSource = macro.QueuedSpells;
            RefreshSpellQueue();
        }

        private void selectedMacro_FlowerQueueChanged(object sender, FlowerQueueItemEventArgs e)
        {
            if (sender is not PlayerMacroState macro)
                return;

            flowerListBox.ItemsSource = macro.FlowerTargets;
            RefreshFlowerQueue();
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not TabControl)
                return;

            if (selectedMacro == null)
                return;

            TabItem oldTab = null;
            TabItem newTab = null;

            if (e.RemovedItems.Count > 0)
                oldTab = e.RemovedItems[0] as TabItem;

            if (e.AddedItems.Count > 0)
                newTab = e.AddedItems[0] as TabItem;

            if (oldTab != null)
                TabDeselected(oldTab);

            if (newTab != null)
                TabSelected(newTab);
        }

        private void TabDeselected(TabItem tab)
        {
            if (selectedMacro == null)
                return;
        }

        private void TabSelected(TabItem tab)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.Client.SelectedTabIndex = tabControl.Items.IndexOf(tab);
            ToggleFlower(selectedMacro.Client.HasLyliacPlant, selectedMacro.Client.HasLyliacVineyard);
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

            if (clientListBox.SelectedItem is not Player player)
                return;

            if (skill.IsEmpty || string.IsNullOrWhiteSpace(skill.Name))
                return;

            logger.LogInfo($"Toggling skill '{skill.Name}' for character: {player.Name}");
            player.Skillbook.ToggleActive(skill.Name);
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

            if (clientListBox.SelectedItem is not Player player)
                return;

            if (spell.IsEmpty || string.IsNullOrWhiteSpace(spell.Name))
                return;

            if (spell.TargetMode == SpellTargetMode.TextInput)
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
            ToggleSpellQueue(true);
            RefreshSpellQueue();

            logger.LogInfo($"Spell '{spell.Name}' added to spell queue for character: {player.Name}");
        }

        private void removeSelectedSpellButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null || spellQueueListBox.SelectedItem is not SpellQueueItem selectedSpell)
                return;

            selectedMacro.RemoveFromSpellQueue(selectedSpell);
            RefreshSpellQueue();

            logger.LogInfo($"Spell '{selectedSpell.Name}' removed from spell queue for character: {selectedMacro.Client.Name}");
        }

        private void removeAllSpellsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.ClearSpellQueue();
            RefreshSpellQueue();

            logger.LogInfo($"Spell queue cleared for character: {selectedMacro.Client.Name}");
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
            RefreshFlowerQueue();

            logger.LogInfo($"Added '{queueItem.Target}' to flower queue for character: {selectedMacro.Client.Name}");
        }

        private void removeSelectedFlowerTargetButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null || flowerListBox.SelectedItem is not FlowerQueueItem selectedTarget)
                return;

            selectedMacro.RemoveFromFlowerQueue(selectedTarget);
            RefreshFlowerQueue();

            logger.LogInfo($"Removed '{selectedTarget.Target}' from flower queue for character: {selectedMacro.Client.Name}");
        }

        private void removeAllFlowerTargetsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.ClearFlowerQueue();
            RefreshFlowerQueue();

            logger.LogInfo($"Cleared flower queue for character: {selectedMacro.Client.Name}");
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

            if (string.Equals(nameof(settings.SkillGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetSkillGridWidth(settings.SkillGridWidth);

            if (string.Equals(nameof(settings.WorldSkillGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetWorldSkillGridWidth(settings.WorldSkillGridWidth);

            if (string.Equals(nameof(settings.SpellGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetSpellGridWidth(settings.SpellGridWidth);

            if (string.Equals(nameof(settings.WorldSpellGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetWorldSpellGridWidth(settings.WorldSpellGridWidth);

            if (string.Equals(nameof(settings.SkillIconSize), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                UpdateSkillSpellGridWidths();

            // Debug settings

            if (string.Equals(nameof(settings.ShowAllProcesses), e.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                PlayerManager.Instance.ShowAllClients = settings.ShowAllProcesses;
                UpdateClientList();
            }
        }

        private void SelectedMacro_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not PlayerMacroState macro)
                return;

            if (string.Equals(nameof(macro.Status), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                UpdateUIForMacroStatus(macro.Status);

            // Update Spell Queue Rotation
            if (string.Equals(nameof(macro.SpellQueueRotation), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                spellQueueRotationComboBox.SelectedValue = macro.SpellQueueRotation;
        }

        private void spellQueueRotationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedMacro == null || e.AddedItems.Count < 1)
                return;

            if (e.AddedItems[0] is not UserSetting selection)
                return;

            if (!Enum.TryParse<SpellRotationMode>(selection.Value as string, out var newRotationMode))
                return;

            selectedMacro.SpellQueueRotation = newRotationMode;
        }

        private void flowerVineyardCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            if (flowerVineyardCheckBox != null)
                selectedMacro.UseLyliacVineyard = flowerVineyardCheckBox.IsChecked.Value;
        }

        private void flowerAlternateCharactersCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            if (flowerAlternateCharactersCheckBox != null)
                selectedMacro.FlowerAlternateCharacters = flowerAlternateCharactersCheckBox.IsChecked.Value;
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

        private void UpdateToolbarState()
        {
            Dispatcher.InvokeIfRequired(() =>
            {
                startNewClientButton.IsEnabled = ClientVersionManager.Instance.Versions.Count(v => v.Key != "Auto-Detect") > 0;

                stopAllMacrosButton.IsEnabled = MacroManager.Instance.Macros.Any(macro => macro.Status == MacroStatus.Running || macro.Status == MacroStatus.Paused);

                if (selectedMacro == null)
                    startMacroButton.IsEnabled = pauseMacroButton.IsEnabled = stopMacroButton.IsEnabled = false;
                else
                    UpdateUIForMacroStatus(selectedMacro.Status);
            }, DispatcherPriority.Normal);
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

        private void ToggleFlower(bool hasLyliacPlant = false, bool hasLyliacVineyard = false)
        {
            flowerTab.IsEnabled = hasLyliacPlant || hasLyliacVineyard;

            flowerAlternateCharactersCheckBox.IsEnabled = hasLyliacPlant;
            flowerVineyardCheckBox.IsEnabled = hasLyliacVineyard;

            if (!hasLyliacPlant)
                flowerAlternateCharactersCheckBox.IsChecked = false;

            if (!hasLyliacVineyard)
                flowerVineyardCheckBox.IsChecked = false;
        }

        private void UpdateClientList()
        {
            var showAll = PlayerManager.Instance.ShowAllClients;
            var sortOrder = PlayerManager.Instance.SortOrder;

            logger.LogInfo($"Updating the client list (showAll = {showAll}, sortOrder = {sortOrder})");

            Dispatcher.InvokeIfRequired(() =>
            {
                clientListBox.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.UpdateTarget();
                clientListBox.Items.Refresh();
            }, DispatcherPriority.DataBind);
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
                logger.LogError($"Unable to check for latest version");
                logger.LogException(ex);

                this.ShowMessageBox("Check for Updates", $"Unable to check for a newer version:\n{ex.Message}", "You can disable this on startup in Settings->Updates.");
                return;
            }
        }
    }
}
