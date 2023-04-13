using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
using System.Runtime.CompilerServices;
using System.IO.Compression;
using System.Linq;

namespace SleepHunter.Views
{
    public partial class MainWindow : Window, IDisposable
    {
        private static readonly int WM_HOTKEY = 0x312;
        private enum ClientLoadResult
        {
            Success = 0,
            ClientPathInvalid,
            HashError,
            AutoDetectFailed,
            BadVersion,
            CreateProcessFailed,
            PatchingFailed
        }

        private static readonly int IconPadding = 14;

        // private static readonly int SkillsTabIndex = 0;
        private static readonly int SpellsTabIndex = 1;
        // private static readonly int FlowerTabIndex = 2;

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

        private Exception loadSkillsException;
        private Exception loadSpellsException;
        private Exception loadStavesException;

        public MainWindow()
        {
            logger = App.Current.Services.GetService<ILogger>();
            releaseService = App.Current.Services.GetService<IReleaseService>();

            LoadSettings();
            InitializeLogger();

            InitializeComponent();
            InitializeViews();

            DisableToolbarButtons();

            LoadVersions();
            LoadThemes();

            LoadSkills();
            LoadSpells();
            LoadStaves();
            CalculateLines();

            ApplyTheme();
            UpdateSkillSpellGridWidths();

            StartUpdateTimers();
        }

        #region IDisposable Methods
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
        #endregion

        #region Client Launch Methods
        private void LaunchClient()
        {
            startNewClientButton.IsEnabled = false;

            var clientPath = UserSettingsManager.Instance.Settings.ClientPath;
            var result = ClientLoadResult.Success;

            logger.LogInfo($"Attempting to launch client executable: {clientPath}");

            try
            {
                // Ensure Client Path Exists
                if (!File.Exists(clientPath))
                {
                    result = ClientLoadResult.ClientPathInvalid;
                    logger.LogError("Client executable not found, unable to launch");
                    return;
                }

                var clientVersion = DetectClientVersion(clientPath, out result);

                if (result != ClientLoadResult.Success)
                    return;

                var processInformation = StartClientProcess(clientPath, out result);

                if (result != ClientLoadResult.Success)
                    return;

                PatchClient(processInformation, clientVersion, out result);
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
                HandleClientLoadResult(result);
                startNewClientButton.IsEnabled = true;
            }
        }

        private ClientVersion DetectClientVersion(string clientPath, out ClientLoadResult result)
        {
            ClientVersion clientVersion = null;
            var clientHash = string.Empty;
            var clientKey = UserSettingsManager.Instance.Settings.SelectedVersion;

            result = ClientLoadResult.Success;
            Stream inputStream = null;

            // Get MD5 Hash and Detect Version
            try
            {
                if (string.Equals("Auto-Detect", clientKey, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInfo($"Attempting to auto-detect client version for executable: {clientPath}");

                    inputStream = File.Open(clientPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using (var md5 = new MD5CryptoServiceProvider())
                    {
                        md5.ComputeHash(inputStream);
                        inputStream.Close();

                        clientHash = BitConverter.ToString(md5.Hash).Replace("-", string.Empty);
                    }

                    clientKey = ClientVersionManager.Instance.DetectVersion(clientHash);

                    if (clientKey == null)
                    {
                        result = ClientLoadResult.AutoDetectFailed;
                        logger.LogError($"No client version known for hash: {clientHash.ToLowerInvariant()}");

                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to calculate client hash");
                logger.LogException(ex);

                result = ClientLoadResult.HashError;
                return clientVersion;
            }
            finally { inputStream?.Dispose(); }

            // Get Version from Manager
            clientVersion = ClientVersionManager.Instance.GetVersion(clientKey);

            if (clientVersion == null)
            {
                result = ClientLoadResult.BadVersion;
                logger.LogWarn($"Unknown client version, key = {clientKey}");
            }
            else
            {
                logger.LogInfo($"Client executable was detected as version: {clientVersion.Key} (md5 = {clientVersion.Hash.ToLowerInvariant()})");
            }

            return clientVersion;
        }

        private ProcessInformation StartClientProcess(string clientPath, out ClientLoadResult result)
        {
            result = ClientLoadResult.Success;

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
               IntPtr.Zero,
               null,
               ref startupInfo, out var processInformation);

            // Ensure the process was actually created
            if (!wasCreated || processInformation.ProcessId == 0)
            {
                result = ClientLoadResult.CreateProcessFailed;
                logger.LogError("Failed to create client process");
            }
            else
            {
                logger.LogInfo($"Created client process successfully with pid {processInformation.ProcessId}");
            }

            return processInformation;
        }

        private void PatchClient(ProcessInformation process, ClientVersion version, out ClientLoadResult result)
        {
            var patchMultipleInstances = UserSettingsManager.Instance.Settings.AllowMultipleInstances;
            var patchIntroVideo = UserSettingsManager.Instance.Settings.SkipIntroVideo;
            var patchNoWalls = UserSettingsManager.Instance.Settings.NoWalls;

            var pid = process.ProcessId;
            logger.LogInfo($"Attempting to patch client process {pid}");

            // Patch Process
            ProcessMemoryAccessor accessor = null;
            Stream patchStream = null;
            BinaryReader reader = null;
            BinaryWriter writer = null;
            try
            {
                accessor = new ProcessMemoryAccessor(pid, ProcessAccess.ReadWrite);
                patchStream = accessor.GetStream();
                reader = new BinaryReader(patchStream);
                writer = new BinaryWriter(patchStream);

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

                result = ClientLoadResult.Success;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to patch client process {pid}");
                logger.LogException(ex);

                result = ClientLoadResult.PatchingFailed;
            }
            finally
            {
                reader?.Dispose();
                writer?.Dispose();
                accessor?.Dispose();
                patchStream?.Dispose();

                // Resume and close handles.
                NativeMethods.ResumeThread(process.ThreadHandle);
                NativeMethods.CloseHandle(process.ThreadHandle);
                NativeMethods.CloseHandle(process.ProcessHandle);
            }
        }

        private void HandleClientLoadResult(ClientLoadResult result)
        {
            // Client Path Invalid
            if (result == ClientLoadResult.ClientPathInvalid)
            {
                var showClientPathSetting = this.ShowMessageBox("Invalid Client Path",
                   "The client path specified in the settings does not exist.\nDo you wish to set it now?",
                   "New clients cannot be started until this value is set to a valid path.",
                   MessageBoxButton.YesNo,
                   460, 260);

                if (showClientPathSetting.Value)
                    ShowSettingsWindow(SettingsWindow.GameClientTabIndex);
            }

            // Hash IO Error
            if (result == ClientLoadResult.HashError)
            {
                this.ShowMessageBox("IO Error",
                   "An I/O error occured when trying to read the client executable.",
                   "You must have read permissions for the file.",
                   MessageBoxButton.OK,
                   460, 240);
            }

            // Auto-Detect Error
            if (result == ClientLoadResult.AutoDetectFailed)
            {
                var showClientVersionSetting = this.ShowMessageBox("Auto-Detect Failed",
                   "The client version could not be detected from the file.\nDo you want to set it manually?",
                   "New clients cannot be started unless version detection is successful.\nYou may manually select a client version instead.",
                   MessageBoxButton.YesNo,
                   460, 260);

                if (showClientVersionSetting.Value)
                    ShowSettingsWindow(SettingsWindow.GameClientTabIndex);
            }

            // Bad- Version Error
            if (result == ClientLoadResult.BadVersion)
            {
                var showClientVersionSetting = this.ShowMessageBox("Invalid Client Version",
                   "The client version selected is invalid.\nWould you like to select another one?",
                   "New clients cannot be started until a valid client version is selected.\nAuto-detection may also be selected.",
                   MessageBoxButton.YesNo,
                   460, 240);

                if (showClientVersionSetting.Value)
                    ShowSettingsWindow(SettingsWindow.GameClientTabIndex);
            }

            // Bad- Version Error
            if (result == ClientLoadResult.BadVersion)
            {
                var showClientVersionSetting = this.ShowMessageBox("Invalid Client Version",
                   "The client version selected is invalid.\nWould you like to select another one?",
                   "New clients cannot be started until a valid client version is selected.\nAuto-detection may also be selected.",
                   MessageBoxButton.YesNo,
                   460, 240);

                if (showClientVersionSetting.Value)
                    ShowSettingsWindow(SettingsWindow.GameClientTabIndex);
            }

            // Create Process
            if (result == ClientLoadResult.CreateProcessFailed)
            {
                var showClientVersionSetting = this.ShowMessageBox("Failed to Launch Client",
                   "An error occured trying to launch the game client.\nDo you want to check the client settings?",
                   "Check that the client path is correct.\nAnti-virus or other security software may be preventing this.",
                   MessageBoxButton.YesNo,
                   420, 240);

                if (showClientVersionSetting.Value)
                    ShowSettingsWindow(SettingsWindow.GameClientTabIndex);
            }

            // Patching
            if (result == ClientLoadResult.PatchingFailed)
            {
                this.ShowMessageBox("Patching Error",
                   "An error occured trying to patch the game client.",
                   "The client should continue to work but features such as multiple instances or skipping the intro video may not be patched properly.\n\nIn some cases the client may crash immediately.",
                   MessageBoxButton.OK,
                   460, 260);
            }
        }
        #endregion

        #region Initialize Methods

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

            Dispatcher.InvokeIfRequired(() =>
               {
                   BindingOperations.GetBindingExpression(clientListBox, ItemsControl.ItemsSourceProperty).UpdateTarget();

               }, DispatcherPriority.DataBind);
        }

        private void OnPlayerCollectionRemove(object sender, PlayerEventArgs e)
        {
            logger.LogInfo($"Game client process removed with pid: {e.Player.Process.ProcessId}");

            OnPlayerLoggedOut(e.Player);

            Dispatcher.InvokeIfRequired(() =>
            {
                BindingOperations.GetBindingExpression(clientListBox, ItemsControl.ItemsSourceProperty).UpdateTarget();

            }, DispatcherPriority.DataBind);

            if (PlayerManager.Instance.Count < 1)
                DisableToolbarButtons();

            if (selectedMacro != null && selectedMacro.Name == e.Player.Name)
                SelectNextAvailablePlayer();
        }

        private void OnPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is Player player))
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

                BindingOperations.GetBindingExpression(clientListBox, ItemsControl.ItemsSourceProperty).UpdateTarget();
                clientListBox.Items.Refresh();

                var selectedPlayer = clientListBox.SelectedItem as Player;

                if (player == selectedPlayer)
                {
                    if (selectedPlayer == null)
                    {
                        ToggleSpellQueue(false);
                        ToggleSkills(false);
                        ToggleSpells(false);
                        ToggleFlower(false);
                    }
                    else
                    {
                        ToggleSpellQueue(tabControl.SelectedIndex == SpellsTabIndex && selectedMacro.TotalSpellsCount > 0);
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

            logger.LogInfo($"Player logged in: {player.Name} (pid {player.Process.ProcessId})");

            if (!string.IsNullOrEmpty(player.Name))
                NativeMethods.SetWindowText(player.Process.WindowHandle, $"Darkages - {player.Name}");

            var shouldRecallMacroState = UserSettingsManager.Instance.Settings.SaveMacroStates;
            var macro = MacroManager.Instance.GetMacroState(player);

            try
            {
                if (shouldRecallMacroState && macro != null)
                {
                    logger.LogInfo($"Attempting to load previous macro state for character: {player.Name}");
                    LoadMacroState(player);
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
        }

        private void OnPlayerLoggedOut(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.Name))
                return;

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
            }

            if (macro != null)
            {
                macro.ClearSpellQueue();
                macro.ClearFlowerQueue();
            }

            if (selectedMacro != null && selectedMacro.Name == player.Name)
                SelectNextAvailablePlayer();
        }

        private void SelectNextAvailablePlayer()
        {
            if (PlayerManager.Instance.Count < 1 || PlayerManager.Instance.Players.All(p => !p.IsLoggedIn))
            {
                clientListBox.SelectedItem = null;
                DisableToolbarButtons();
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

            spellQueueListBox.ItemsSource = selectedMacro.QueuedSpells;
            spellQueueListBox.Items.Refresh();
        }

        private void RefreshFlowerQueue()
        {
            if (!CheckAccess())
            {
                Dispatcher.InvokeIfRequired(RefreshFlowerQueue, DispatcherPriority.DataBind);
                return;
            }

            flowerListBox.ItemsSource = selectedMacro.FlowerTargets;
            flowerListBox.Items.Refresh();
        }

        private void LoadVersions()
        {
            var versionsFile = ClientVersionManager.VersionsFile;
            logger.LogInfo($"Attempting to load client versions from file: {versionsFile}");

            try
            {
                if (File.Exists(versionsFile))
                {
                    ClientVersionManager.Instance.LoadFromFile(versionsFile);
                    logger.LogInfo("Client versions successfully loaded");
                }
                else
                {
                    ClientVersionManager.Instance.LoadDefaultVersions();
                    logger.LogInfo("No client version file was found, using defaults");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load client versions, resetting to defaults");
                logger.LogException(ex);

                ClientVersionManager.Instance.LoadDefaultVersions();
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
                    logger.LogInfo("Themes successfully lodaded");
                }
                else
                {

                    ColorThemeManager.Instance.LoadDefaultThemes();
                    logger.LogInfo("No themes file was found, using defaults");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load themes, resetting to defaults");
                logger.LogException(ex);

                ColorThemeManager.Instance.LoadDefaultThemes();
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
                    logger.LogInfo("Skill metadata successfully loaded");
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

                loadSkillsException = ex;
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
                    logger.LogInfo("Spell metadata successfully loaded");
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

                loadSpellsException = ex;
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
                    logger.LogInfo("Staves metadata successfully loaded");
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

                loadStavesException = ex;
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
        #endregion

        #region Client Add/Remove/Update Methods
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
        #endregion

        private void ToggleModalOverlay(bool showHide) => modalOverlay.Visibility = showHide ? Visibility.Visible : Visibility.Hidden;

        private void ToggleSpellQueue(bool showQueue)
        {
            if (spellQueueListBox == null)
                return;

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
            Process.Start(updaterExecutable, $"{updateFile} {installationPath}");
            Application.Current.Shutdown();
        }

        private IntPtr WindowMessageHook(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam, ref bool isHandled)
        {
            if (message == WM_HOTKEY)
            {
                var key = KeyInterop.KeyFromVirtualKey(lParam.ToInt32() >> 16);
                var modifiers = (ModifierKeys)(lParam.ToInt32() & 0xFFFF);

                ActivateHotkey(key, modifiers);
            }

            return IntPtr.Zero;
        }

        private void Window_Shown(object sender, EventArgs e)
        { 
            InitializeHotkeyHook();

            if (loadSkillsException != null)
            {
                this.ShowMessageBox("Load Error",
                   string.Format("There was an error loading skill metadata from file:\n{0}", loadSkillsException.Message),
                   "Information for skills is required for some features to work properly.",
                   MessageBoxButton.OK,
                   440, 280);
            }

            if (loadSpellsException != null)
            {
                this.ShowMessageBox("Load Error",
                   string.Format("There was an error loading spell metadata from file:\n{0}", loadSpellsException.Message),
                   "Information for spells is required for some casting features to work properly.",
                   MessageBoxButton.OK,
                   440, 280);
            }

            if (loadStavesException != null)
            {
                this.ShowMessageBox("Load Error",
                   string.Format("There was an error loading staff metadata from file:\n{0}", loadStavesException.Message),
                   "Information on staves is required for some casting features to work properly.",
                   MessageBoxButton.OK,
                   440, 280);
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
                logger.LogInfo("Unregistering all hotkeys");
                HotkeyManager.Instance.UnregisterAllHotkeys(windowSource.Handle);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to unregister all hotkeys");
                logger.LogException(ex);
            }

            try
            {
                var versionsFile = ClientVersionManager.VersionsFile;

                if (!File.Exists(versionsFile))
                {
                    logger.LogInfo($"Client versions file does not exist, saving to file: {versionsFile}");
                    ClientVersionManager.Instance.SaveToFile(versionsFile);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to save client versions file");
                logger.LogException(ex);
            }

            try
            {
                var themesFile = ColorThemeManager.ThemesFile;

                if (!File.Exists(themesFile))
                {
                    logger.LogInfo($"Themes file does not exist, saving to file: {themesFile}");
                    ColorThemeManager.Instance.SaveToFile(ColorThemeManager.ThemesFile);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to save themes file");
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
                var skillsFile = SkillMetadataManager.SkillMetadataFile;

                logger.LogInfo($"Saving skills metadata to file: {skillsFile}");
                SkillMetadataManager.Instance.SaveToFile(skillsFile);
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to save skills metadata file");
                logger.LogException(ex);
            }

            try
            {
                var spellsFile = SpellMetadataManager.SpellMetadataFile;

                logger.LogInfo($"Saving spells metadata to file: {spellsFile}");
                SpellMetadataManager.Instance.SaveToFile(spellsFile);
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to save spells metadata file");
                logger.LogException(ex);
            }

            try
            {
                var stavesFile = StaffMetadataManager.StaffMetadataFile;

                logger.LogInfo($"Saving staves metadata to file: {stavesFile}");
                StaffMetadataManager.Instance.SaveToFile(stavesFile);
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to save staves metadata file");
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

        private void DisableToolbarButtons()
        {
            startMacroButton.IsEnabled = false;
            pauseMacroButton.IsEnabled = false;
            stopMacroButton.IsEnabled = false;
        }

        private void SaveMacroState(PlayerMacroState macro)
        {
            if (macro == null)
                throw new ArgumentNullException("macro");

            var filename = Path.Combine("saves", string.Format("{0}.xml", macro.Client.Name.Trim()));
            var state = new SavedMacroState(macro);

            logger.LogInfo($"Saving macro state to file: {filename}");
            state.SaveToFile(filename);
        }

        private void LoadMacroState(Player player)
        {
            if (player == null)
                throw new ArgumentNullException("player");

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
        }

        #region Toolbar Button Click Methods
        private void startNewClientButton_Click(object sender, RoutedEventArgs e) => LaunchClient();

        private void startMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null || selectedMacro.Client == null || !selectedMacro.Client.IsLoggedIn)
                return;

            selectedMacro.Client.Update(PlayerFieldFlags.Location);
            selectedMacro.Start();

            logger.LogInfo($"Started macro state for character: {selectedMacro.Client.Name} (toolbar)");
        }

        private void pauseMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.Pause();
            logger.LogInfo($"Paused macro state for character {selectedMacro.Client.Name} (toolbar)");
        }

        private void stopMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.Stop();
            logger.LogInfo($"Stopped macro state for character {selectedMacro.Client.Name} (toolbar)");
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e) => ShowSettingsWindow();
        #endregion

        private void clientListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListBoxItem listBoxItem))
                return;

            if (!(listBoxItem.Content is Player player))
                return;

            NativeMethods.SetForegroundWindow(player.Process.WindowHandle);
            logger.LogInfo($"Setting foreground window for client: {player.Name} (double-click)");
        }

        private void spellQueueListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListBoxItem listBoxItem))
                return;

            if (!(listBoxItem.Content is SpellQueueItem queueItem))
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
            if (!(sender is ListBoxItem listBoxItem))
                return;

            if (!(listBoxItem.Content is SpellQueueItem spell))
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
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var draggedItem = sender as ListBoxItem;
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        private void spellQueueListBox_Drop(object sender, DragEventArgs e)
        {
            var droppedItem = e.Data.GetData(typeof(SpellQueueItem)) as SpellQueueItem;
            var target = (sender as ListBoxItem)?.DataContext as SpellQueueItem;

            var removedIndex = spellQueueListBox.Items.IndexOf(droppedItem);
            var targetIndex = spellQueueListBox.Items.IndexOf(target);

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
        }

        private void flowerQueueListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListBoxItem listBoxItem))
                return;

            if (!(listBoxItem.Content is FlowerQueueItem queueItem))
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
            if (!(sender is ListBoxItem listBoxItem))
                return;

            if (!(listBoxItem.Content is FlowerQueueItem flower))
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
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var draggedItem = sender as ListBoxItem;
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        private void flowerQueueListBox_Drop(object sender, DragEventArgs e)
        {
            var droppedItem = e.Data.GetData(typeof(FlowerQueueItem)) as FlowerQueueItem;
            var target = (sender as ListBoxItem)?.DataContext as FlowerQueueItem;

            var removedIndex = flowerListBox.Items.IndexOf(droppedItem);
            var targetIndex = flowerListBox.Items.IndexOf(target);

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
        }

        private void clientListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ListBox listBox))
            {
                if (selectedMacro != null)
                    selectedMacro.PropertyChanged -= SelectedMacro_PropertyChanged;

                Title = "SleepHunter";
                selectedMacro = null;
                ToggleSpellQueue(false);
                ToggleSkills(false);
                ToggleSpells(false);
                ToggleFlower();
                return;
            }

            if (!(listBox.SelectedItem is Player player))
            {
                if (selectedMacro != null)
                    selectedMacro.PropertyChanged -= SelectedMacro_PropertyChanged;

                Title = "SleepHunter";
                selectedMacro = null;
                ToggleSpellQueue(false);
                ToggleSkills(false);
                ToggleSpells(false);
                ToggleFlower();
                return;
            }

            var macroState = MacroManager.Instance.GetMacroState(player);

            if (player.IsLoggedIn)
            {
                Title = string.Format("SleepHunter - {0}", player.Name);
                logger.LogInfo($"Selected charcter: {player.Name}");
            }
            else Title = "SleepHunter";

            if (macroState == null)
            {
                if (selectedMacro != null)
                    selectedMacro.PropertyChanged -= SelectedMacro_PropertyChanged;

                selectedMacro = null;

                startMacroButton.IsEnabled = pauseMacroButton.IsEnabled = stopMacroButton.IsEnabled = false;
                return;
            }

            if (selectedMacro != null)
            {
                selectedMacro.PropertyChanged -= SelectedMacro_PropertyChanged;

                selectedMacro.SpellAdded -= selectedMacro_SpellQueueChanged;
                selectedMacro.SpellUpdated -= selectedMacro_SpellQueueChanged;
                selectedMacro.SpellRemoved -= selectedMacro_SpellQueueChanged;

                selectedMacro.FlowerTargetAdded -= selectedMacro_FlowerQueueChanged;
                selectedMacro.FlowerTargetUpdated -= selectedMacro_FlowerQueueChanged;
                selectedMacro.FlowerTargetRemoved -= selectedMacro_FlowerQueueChanged;
            }

            selectedMacro = macroState;

            if (selectedMacro != null)
            {
                selectedMacro.PropertyChanged += SelectedMacro_PropertyChanged;

                selectedMacro.SpellAdded += selectedMacro_SpellQueueChanged;
                selectedMacro.SpellUpdated += selectedMacro_SpellQueueChanged;
                selectedMacro.SpellRemoved += selectedMacro_SpellQueueChanged;

                selectedMacro.FlowerTargetAdded += selectedMacro_FlowerQueueChanged;
                selectedMacro.FlowerTargetUpdated += selectedMacro_FlowerQueueChanged;
                selectedMacro.FlowerTargetRemoved += selectedMacro_FlowerQueueChanged;
            }

            tabControl.SelectedIndex = selectedMacro.Client.SelectedTabIndex;

            ToggleSkills(player.IsLoggedIn);
            ToggleSpells(player.IsLoggedIn);
            ToggleFlower(player.HasLyliacPlant, player.HasLyliacVineyard);

            if (selectedMacro.Client.IsLoggedIn)
                UpdateUIForMacroStatus(selectedMacro.Status);
            else
                DisableToolbarButtons();

            if (selectedMacro != null)
            {
                ToggleSpellQueue(tabControl.SelectedIndex == SpellsTabIndex && selectedMacro.TotalSpellsCount > 0);
                spellQueueListBox.ItemsSource = selectedMacro.QueuedSpells;
                flowerListBox.ItemsSource = selectedMacro.FlowerTargets;

                flowerVineyardCheckBox.IsChecked = selectedMacro.UseLyliacVineyard && player.HasLyliacVineyard;
                flowerAlternateCharactersCheckBox.IsChecked = selectedMacro.FlowerAlternateCharacters && player.HasLyliacPlant;

                foreach (var spell in selectedMacro.QueuedSpells)
                    spell.IsUndefined = !SpellMetadataManager.Instance.ContainsSpell(spell.Name);
            }
        }

        private void clientListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.None)
                return;

            if (!(sender is ListBoxItem listBoxItem))
                return;

            if (!(listBoxItem.Content is Player player))
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
                foreach (var p in PlayerManager.Instance.Players)
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
            if (!(sender is PlayerMacroState macro))
                return;

            spellQueueListBox.ItemsSource = macro.QueuedSpells;
            RefreshSpellQueue();

            ToggleSpellQueue(tabControl.SelectedIndex == SpellsTabIndex && macro.TotalSpellsCount > 0);
        }

        private void selectedMacro_FlowerQueueChanged(object sender, FlowerQueueItemEventArgs e)
        {
            if (!(sender is PlayerMacroState macro))
                return;

            flowerListBox.ItemsSource = macro.FlowerTargets;
            RefreshFlowerQueue();
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is TabControl))
            {
                ToggleSpellQueue(false);
                return;
            }

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

            ToggleSpellQueue(selectedMacro.TotalSpellsCount > 0);
            ToggleFlower(selectedMacro.Client.HasLyliacPlant, selectedMacro.Client.HasLyliacVineyard);
        }

        private void skillListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListBoxItem item))
                return;

            if (!(item.Content is Skill skill))
                return;

            if (!(clientListBox.SelectedItem is Player player))
                return;

            if (skill.IsEmpty || string.IsNullOrWhiteSpace(skill.Name))
                return;

            logger.LogInfo($"Toggling skill '{skill.Name}' for character: {player.Name}");
            player.Skillbook.ToggleActive(skill.Name);
        }

        private void spellListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListBoxItem item))
                return;

            if (!(item.Content is Spell spell))
                return;

            if (!(clientListBox.SelectedItem is Player player))
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
            logger.LogInfo($"Spell '{spell.Name}' added to spell queue for character: {player.Name}");
        }

        private void removeSelectedSpellButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null || !(spellQueueListBox.SelectedItem is SpellQueueItem selectedSpell))
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

            logger.LogInfo($"Added '{queueItem.Target}' to flower queue for character: {selectedMacro.Client.Name}");
        }

        private void removeSelectedFlowerTargetButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedTarget = flowerListBox.SelectedItem as FlowerQueueItem;

            if (selectedMacro == null || selectedTarget == null)
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
            if (!(sender is UserSettings settings))
                return;

            logger.LogInfo($"User setting property changed: {e.PropertyName}");

            if (string.Equals("SelectedTheme", e.PropertyName, StringComparison.OrdinalIgnoreCase))
                ApplyTheme();

            if (string.Equals("RainbowMode", e.PropertyName, StringComparison.OrdinalIgnoreCase))
                ApplyTheme();

            if (string.Equals("SkillGridWidth", e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetSkillGridWidth(settings.SkillGridWidth);

            if (string.Equals("WorldSkillGridWidth", e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetWorldSkillGridWidth(settings.WorldSkillGridWidth);

            if (string.Equals("SpellGridWidth", e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetSpellGridWidth(settings.SpellGridWidth);

            if (string.Equals("WorldSpellGridWidth", e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetWorldSpellGridWidth(settings.WorldSpellGridWidth);

            if (string.Equals("SkillIconSize", e.PropertyName, StringComparison.OrdinalIgnoreCase))
                UpdateSkillSpellGridWidths();
        }

        private void SelectedMacro_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is PlayerMacroState macro))
                return;

            if (string.Equals("Status", e.PropertyName, StringComparison.OrdinalIgnoreCase))
                UpdateUIForMacroStatus(macro.Status);
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

        private void ToggleSkills(bool show = true)
        {
            temuairSkillListBox.Visibility = medeniaSkillListBox.Visibility = worldSkillListBox.Visibility = (show ? Visibility.Visible : Visibility.Collapsed);
            skillsTab.IsEnabled = show;
        }

        private void ToggleSpells(bool show = true)
        {
            temuairSpellListBox.Visibility = medeniaSpellListBox.Visibility = worldSpellListBox.Visibility = (show ? Visibility.Visible : Visibility.Collapsed);
            spellsTab.IsEnabled = show;
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
