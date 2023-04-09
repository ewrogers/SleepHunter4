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

namespace SleepHunter.Views
{
    public partial class MainWindow : Window, IDisposable
    {
        static readonly int WM_HOTKEY = 0x312;

        enum ClientLoadResult
        {
            Success = 0,
            ClientPathInvalid,
            HashError,
            AutoDetectFailed,
            BadVersion,
            CreateProcessFailed,
            PatchingFailed
        }

        static readonly int IconPadding = 14;

        // static readonly int SkillsTabIndex = 0;
        static readonly int SpellsTabIndex = 1;
        // static readonly int FlowerTabIndex = 2;

        bool isDisposed;
        HwndSource windowSource;

        int recentSettingsTabIndex;
        MetadataEditorWindow metadataWindow;
        SettingsWindow settingsWindow;
        BackgroundWorker processUpdateWorker;
        BackgroundWorker clientUpdateWorker;
        BackgroundWorker flowerUpdateWorker;

        PlayerMacroState selectedMacro;

        Exception loadSkillsException;
        Exception loadSpellsException;
        Exception loadStavesException;

        public MainWindow()
        {
            InitializeComponent();
            InitializeViews();

            LoadVersions();
            LoadThemes();
            LoadSettings();

            LoadSkills();
            LoadSpells();
            LoadStaves();
            CalculateLines();

            ApplyTheme();
            ApplySettings();
            ApplyDebugSettings();

            StartUpdateTimers();
        }

        #region IDisposable Methods
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                if (processUpdateWorker != null)
                    processUpdateWorker.Dispose();

                if (clientUpdateWorker != null)
                    clientUpdateWorker.Dispose();

                if (flowerUpdateWorker != null)
                    flowerUpdateWorker.Dispose();
            }

            if (windowSource != null)
                windowSource.Dispose();

            isDisposed = true;
        }
        #endregion

        #region Client Launch Methods
        void LaunchClient()
        {
            startNewClientButton.IsEnabled = false;

            var clientPath = UserSettingsManager.Instance.Settings.ClientPath;
            var clientHash = string.Empty;
            var result = ClientLoadResult.Success;

            try
            {
                // Ensure Client Path Exists
                if (!File.Exists(clientPath))
                {
                    result = ClientLoadResult.ClientPathInvalid;
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
                this.ShowMessageBox(ex.GetType().Name,
                   ex.Message, "This error occured trying to launch a new client.",
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

        ClientVersion DetectClientVersion(string clientPath, out ClientLoadResult result)
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
                        return clientVersion;
                    }
                }
            }
            catch
            {
                result = ClientLoadResult.HashError;
                return clientVersion;
            }
            finally { inputStream?.Dispose(); }

            // Get Version from Manager
            clientVersion = ClientVersionManager.Instance.GetVersion(clientKey);

            if (clientVersion == null)
                result = ClientLoadResult.BadVersion;

            return clientVersion;
        }


        ProcessInformation StartClientProcess(string clientPath, out ClientLoadResult result)
        {
            result = ClientLoadResult.Success;

            // Create Process
            ProcessInformation processInformation;
            var startupInfo = new StartupInfo { Size = Marshal.SizeOf(typeof(StartupInfo)) };

            var processSecurity = new SecurityAttributes();
            var threadSecurity = new SecurityAttributes();

            processSecurity.Size = Marshal.SizeOf(processSecurity);
            threadSecurity.Size = Marshal.SizeOf(threadSecurity);

            bool wasCreated = NativeMethods.CreateProcess(clientPath,
               null,
               ref processSecurity, ref threadSecurity,
               false,
               ProcessCreationFlags.Suspended,
               IntPtr.Zero,
               null,
               ref startupInfo, out processInformation);

            // Check Process
            if (!wasCreated || processInformation.ProcessId == 0)
                result = ClientLoadResult.CreateProcessFailed;

            return processInformation;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        void PatchClient(ProcessInformation process, ClientVersion version, out ClientLoadResult result)
        {
            var patchMultipleInstances = UserSettingsManager.Instance.Settings.AllowMultipleInstances;
            var patchIntroVideo = UserSettingsManager.Instance.Settings.SkipIntroVideo;
            var patchNoWalls = UserSettingsManager.Instance.Settings.NoWalls;

            // Patch Process
            ProcessMemoryAccessor accessor = null;
            Stream patchStream = null;
            BinaryReader reader = null;
            BinaryWriter writer = null;
            try
            {
                accessor = new ProcessMemoryAccessor(process.ProcessId, ProcessAccess.ReadWrite);
                patchStream = accessor.GetStream();
                reader = new BinaryReader(patchStream);
                writer = new BinaryWriter(patchStream);

                if (patchMultipleInstances && version.MultipleInstanceAddress > 0)
                {
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
                    patchStream.Position = version.NoWallAddress;
                    writer.Write((byte)0xEB);        // JMP SHORT
                    writer.Write((byte)0x17);        // +0x17
                    writer.Write((byte)0x90);        // NOP
                }

                result = ClientLoadResult.Success;
            }
            catch
            {
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

        void HandleClientLoadResult(ClientLoadResult result)
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

        void InitializeHotkeyHook()
        {
            var helper = new WindowInteropHelper(this);
            windowSource = HwndSource.FromHwnd(helper.Handle);

            windowSource.AddHook(WindowMessageHook);
        }

        void InitializeViews()
        {
            PlayerManager.Instance.PlayerAdded += OnPlayerCollectionAdd;
            PlayerManager.Instance.PlayerUpdated += OnPlayerCollectionAdd;
            PlayerManager.Instance.PlayerRemoved += OnPlayerCollectionRemove;

            PlayerManager.Instance.PlayerPropertyChanged += OnPlayerPropertyChanged;

            SpellMetadataManager.Instance.SpellAdded += OnSpellManagerUpdated;
            SpellMetadataManager.Instance.SpellChanged += OnSpellManagerUpdated;
            SpellMetadataManager.Instance.SpellRemoved += OnSpellManagerUpdated;
        }

        void OnSpellManagerUpdated(object sender, SpellMetadataEventArgs e)
        {
            if (selectedMacro == null)
                return;

            foreach (var spell in selectedMacro.QueuedSpells)
                spell.IsUndefined = !SpellMetadataManager.Instance.ContainsSpell(spell.Name);
        }

        void OnPlayerCollectionAdd(object sender, PlayerEventArgs e)
        {
            this.Dispatcher.InvokeIfRequired(() =>
               {
                   BindingOperations.GetBindingExpression(clientListBox, ListView.ItemsSourceProperty).UpdateTarget();

               }, DispatcherPriority.DataBind);
        }

        void OnPlayerCollectionRemove(object sender, PlayerEventArgs e)
        {
            OnPlayerLoggedOut(e.Player);

            this.Dispatcher.InvokeIfRequired(() =>
            {
                BindingOperations.GetBindingExpression(clientListBox, ListView.ItemsSourceProperty).UpdateTarget();

            }, DispatcherPriority.DataBind);
        }

        void OnPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var player = sender as Player;
            if (player == null)
                return;

            this.Dispatcher.InvokeIfRequired(() =>
            {
                if (string.Equals("IsLoggedIn", e.PropertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!player.IsLoggedIn)
                        OnPlayerLoggedOut(player);
                    else
                        OnPlayerLoggedIn(player);
                }

                BindingOperations.GetBindingExpression(clientListBox, ListView.ItemsSourceProperty).UpdateTarget();
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

        void OnPlayerLoggedIn(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.Name))
                return;

            var shouldSaveMacroStates = UserSettingsManager.Instance.Settings.SaveMacroStates;
            var macro = MacroManager.Instance.GetMacroState(player);

            try
            {
                if (shouldSaveMacroStates && macro != null)
                {
                    LoadMacroState(player);
                }
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke((Action)delegate
                {
                    this.ShowMessageBox("Load State Error",
                   string.Format("There was an error loading the macro state:\n{0}", ex.Message),
                   string.Format("{0}'s macro state was not preserved.", player.Name),
                   MessageBoxButton.OK, 460, 260);

                }, DispatcherPriority.Normal, null);
            }
        }

        void OnPlayerLoggedOut(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.Name))
                return;

            var shouldSaveMacroStates = UserSettingsManager.Instance.Settings.SaveMacroStates;
            var macro = MacroManager.Instance.GetMacroState(player);

            try
            {
                if (shouldSaveMacroStates && macro != null)
                    SaveMacroState(macro);

            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke((Action)delegate
                {
                    this.ShowMessageBox("Save State Error",
                   string.Format("There was an error saving the macro state:\n{0}", ex.Message),
                   string.Format("{0}'s macro state may not be preserved upon next login.", player.Name),
                   MessageBoxButton.OK, 460, 260);

                }, DispatcherPriority.Normal, null);
            }
            finally
            {
                this.Dispatcher.InvokeIfRequired(() =>
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
        }

        void RefreshSpellQueue()
        {
            if (!CheckAccess())
            {
                this.Dispatcher.InvokeIfRequired((Action)RefreshSpellQueue, DispatcherPriority.DataBind);
                return;
            }

            spellQueueListBox.ItemsSource = selectedMacro.QueuedSpells;
            spellQueueListBox.Items.Refresh();
        }

        void RefreshFlowerQueue()
        {
            if (!CheckAccess())
            {
                this.Dispatcher.InvokeIfRequired((Action)RefreshFlowerQueue, DispatcherPriority.DataBind);
                return;
            }

            flowerListBox.ItemsSource = selectedMacro.FlowerTargets;
            flowerListBox.Items.Refresh();
        }

        void LoadVersions()
        {
            try
            {
                if (File.Exists(ClientVersionManager.VersionsFile))
                    ClientVersionManager.Instance.LoadFromFile(ClientVersionManager.VersionsFile);
                else
                    ClientVersionManager.Instance.LoadDefaultVersions();
            }
            catch
            {
                ClientVersionManager.Instance.LoadDefaultVersions();
            }
        }

        void LoadThemes()
        {
            try
            {
                if (File.Exists(ColorThemeManager.ThemesFile))
                    ColorThemeManager.Instance.LoadFromFile(ColorThemeManager.ThemesFile);
                else
                    ColorThemeManager.Instance.LoadDefaultThemes();
            }
            catch
            {
                ColorThemeManager.Instance.LoadDefaultThemes();
            }
        }

        void LoadSettings()
        {
            try
            {
                if (File.Exists(UserSettingsManager.SettingsFile))
                    UserSettingsManager.Instance.LoadFromFile(UserSettingsManager.SettingsFile);
                else
                    UserSettingsManager.Instance.Settings.ResetDefaults();
            }
            catch
            {
                UserSettingsManager.Instance.Settings.ResetDefaults();
            }
            finally
            {
                UserSettingsManager.Instance.Settings.PropertyChanged += UserSettings_PropertyChanged;
            }
        }

        void LoadSkills()
        {
            try
            {
                if (File.Exists(SkillMetadataManager.SkillMetadataFile))
                    SkillMetadataManager.Instance.LoadFromFile(SkillMetadataManager.SkillMetadataFile);
            }
            catch (Exception ex)
            {
                loadSkillsException = ex;
            }
        }

        void LoadSpells()
        {
            try
            {
                if (File.Exists(SpellMetadataManager.SpellMetadataFile))
                    SpellMetadataManager.Instance.LoadFromFile(SpellMetadataManager.SpellMetadataFile);
            }
            catch (Exception ex)
            {
                loadSpellsException = ex;
            }
        }

        void LoadStaves()
        {
            try
            {
                if (File.Exists(StaffMetadataManager.StaffMetadataFile))
                    StaffMetadataManager.Instance.LoadFromFile(StaffMetadataManager.StaffMetadataFile);
            }
            catch (Exception ex)
            {
                loadStavesException = ex;
            }
        }

        void CalculateLines()
        {
            StaffMetadataManager.Instance.RecalculateAllStaves();
        }

        void StartUpdateTimers()
        {
            IconManager.Instance.Context = TaskScheduler.FromCurrentSynchronizationContext();

            StartProcessUpdate();
            StartClientUpdate();
            StartFlowerUpdate();
        }

        void StartProcessUpdate()
        {
            processUpdateWorker = new BackgroundWorker();
            processUpdateWorker.WorkerSupportsCancellation = true;

            processUpdateWorker.DoWork += (sender, e) =>
               {
                   var delayTime = (TimeSpan)e.Argument;

                   if (delayTime > TimeSpan.Zero)
                       Thread.Sleep(delayTime);

                   ProcessManager.Instance.ScanForProcesses();
               };

            processUpdateWorker.RunWorkerCompleted += (sender, e) =>
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

                   processUpdateWorker.RunWorkerAsync(UserSettingsManager.Instance.Settings.ProcessUpdateInterval);
               };

            // Start immediately!
            processUpdateWorker.RunWorkerAsync(TimeSpan.Zero);
        }

        void StartClientUpdate()
        {
            clientUpdateWorker = new BackgroundWorker();
            clientUpdateWorker.WorkerSupportsCancellation = true;

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
        }

        void StartFlowerUpdate()
        {
            flowerUpdateWorker = new BackgroundWorker();
            flowerUpdateWorker.WorkerReportsProgress = true;

            var updateInterval = TimeSpan.FromMilliseconds(100);

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

            flowerUpdateWorker.RunWorkerAsync(TimeSpan.Zero);
        }

        void ApplyTheme()
        {
            if (!UserSettingsManager.Instance.Settings.RainbowMode)
                ColorThemeManager.Instance.ApplyTheme(UserSettingsManager.Instance.Settings.SelectedTheme);
            else
                ColorThemeManager.Instance.ApplyRainbowMode();
        }

        void ApplySettings()
        {
            UpdateSkillSpellGridWidths();
        }

        void ActivateHotkey(Key key, ModifierKeys modifiers)
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

            var macroState = MacroManager.Instance.GetMacroState(hotkeyPlayer);

            if (macroState == null)
                return;

            if (macroState.Status == MacroStatus.Running)
                macroState.Pause();
            else
            {
                hotkeyPlayer.Update(PlayerFieldFlags.Location);
                macroState.Start();
            }
        }

        [Conditional("DEBUG")]
        void ApplyDebugSettings()
        {
            var settings = UserSettingsManager.Instance.Settings.IsDebugMode = true;
        }
        #endregion

        #region Client Add/Remove/Update Methods
        void UpdateSkillSpellGridWidths()
        {
            var settings = UserSettingsManager.Instance.Settings;

            SetSkillGridWidth(settings.SkillGridWidth);
            SetWorldSkillGridWidth(settings.WorldSkillGridWidth);
            SetSpellGridWidth(settings.SpellGridWidth);
            SetWorldSpellGridWidth(settings.WorldSpellGridWidth);
        }

        void SetSkillGridWidth(int units)
        {
            if (units < 1)
            {
                temuairSkillListBox.MaxWidth = medeniaSkillListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.SkillIconSize;
            temuairSkillListBox.MaxWidth = medeniaSkillListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        void SetWorldSkillGridWidth(int units)
        {
            if (units < 1)
            {
                worldSkillListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.SkillIconSize;
            worldSkillListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        void SetSpellGridWidth(int units)
        {
            if (units < 1)
            {
                temuairSpellListBox.MaxWidth = medeniaSpellListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.SkillIconSize;
            temuairSpellListBox.MaxWidth = medeniaSpellListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        void SetWorldSpellGridWidth(int units)
        {
            if (units < 1)
            {
                worldSpellListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.SkillIconSize;
            worldSpellListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        void UpdateUIForMacroStatus(MacroStatus status)
        {
            if (!CheckAccess())
            {
                this.Dispatcher.InvokeIfRequired<MacroStatus>(UpdateUIForMacroStatus, status, DispatcherPriority.DataBind);
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

        void ToggleSpellQueue(bool showQueue)
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
                metadataWindow = new MetadataEditorWindow();
                metadataWindow.Owner = this;
            }

            if (selectedTabIndex >= 0)
                metadataWindow.SelectedTabIndex = selectedTabIndex;

            metadataWindow.Show();
        }

        public void ShowSettingsWindow(int selectedTabIndex = -1)
        {
            if (settingsWindow == null || !settingsWindow.IsLoaded)
            {
                settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
            }

            if (selectedTabIndex >= 0)
                settingsWindow.SelectedTabIndex = selectedTabIndex;
            else
                settingsWindow.SelectedTabIndex = recentSettingsTabIndex;

            settingsWindow.Closing += (sender, e) => { recentSettingsTabIndex = (sender as SettingsWindow).SelectedTabIndex; };
            settingsWindow.Show();
        }

        IntPtr WindowMessageHook(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam, ref bool isHandled)
        {
            if (message == WM_HOTKEY)
            {
                var key = KeyInterop.KeyFromVirtualKey(lParam.ToInt32() >> 16);
                var modifiers = (ModifierKeys)(lParam.ToInt32() & 0xFFFF);

                ActivateHotkey(key, modifiers);
            }

            return IntPtr.Zero;
        }

        void Window_Shown(object sender, EventArgs e)
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
                CheckForUpdate(false);
        }

        void Window_Closing(object sender, CancelEventArgs e)
        {
            UserSettingsManager.Instance.Settings.PropertyChanged -= UserSettings_PropertyChanged;

            try
            {
                HotkeyManager.Instance.UnregisterAllHotkeys(windowSource.Handle);
            }
            catch { }

            try
            { ClientVersionManager.Instance.SaveToFile(ClientVersionManager.VersionsFile); }
            catch (Exception ex)
            {
                this.ShowMessageBox("Save Error",
                   string.Format("There was an error saving client versions to file:\n{0}", ex.Message),
                   "You may have to reset client versions the next time you start the program.",
                   MessageBoxButton.OK,
                   440, 280);
            }

            try
            { ColorThemeManager.Instance.SaveToFile(ColorThemeManager.ThemesFile); }
            catch (Exception ex)
            {
                this.ShowMessageBox("Save Error",
                   string.Format("There was an error saving color themes to file:\n{0}", ex.Message),
                   "You may have to reset color themes the next time you start the program.",
                   MessageBoxButton.OK,
                   440, 280);
            }

            try
            { UserSettingsManager.Instance.SaveToFile(UserSettingsManager.SettingsFile); }
            catch (Exception ex)
            {
                this.ShowMessageBox("Save Error",
                   string.Format("There was an error saving user settings to file:\n{0}", ex.Message),
                   "User settings may be reset to the defaults next time you start the program.",
                   MessageBoxButton.OK,
                   440, 280);
            }

            try
            { SkillMetadataManager.Instance.SaveToFile(SkillMetadataManager.SkillMetadataFile); }
            catch (Exception ex)
            {
                this.ShowMessageBox("Save Error",
                   string.Format("There was an error saving skill metadata to file:\n{0}", ex.Message),
                   "Information on skills may be lost and cause the program to not run properly.",
                   MessageBoxButton.OK,
                   440, 280);
            }

            try
            { SpellMetadataManager.Instance.SaveToFile(SpellMetadataManager.SpellMetadataFile); }
            catch (Exception ex)
            {
                this.ShowMessageBox("Save Error",
                   string.Format("There was an error saving spell metadata to file:\n{0}", ex.Message),
                   "Information on spells may be lost and cause the program to not run properly.",
                   MessageBoxButton.OK,
                   440, 280);
            }

            try
            { StaffMetadataManager.Instance.SaveToFile(StaffMetadataManager.StaffMetadataFile); }
            catch (Exception ex)
            {
                this.ShowMessageBox("Save Error",
                   string.Format("There was an error saving staff metadata to file:\n{0}", ex.Message),
                   "Information on staves may be lost and cause the program to not run properly.",
                   MessageBoxButton.OK,
                   440, 280);
            }

            try
            { FileArchiveManager.Instance.ClearArchives(); }
            catch { }

            try
            {
                var shouldSaveStates = UserSettingsManager.Instance.Settings.SaveMacroStates;

                if (!Directory.Exists("saves"))
                    Directory.CreateDirectory("saves");

                if (shouldSaveStates)
                {
                    foreach (var macro in MacroManager.Instance.Macros)
                    {
                        if (macro.Client == null || !macro.Client.IsLoggedIn)
                            continue;

                        if (!string.IsNullOrEmpty(macro.Client.Name))
                            SaveMacroState(macro);
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageBox("Save State Error",
                   string.Format("There was an error saving macro state:\n{0}", ex.Message),
                   "Your character macro states may not be preserved properly.",
                   MessageBoxButton.OK,
                   440, 280);
            }
        }

        void SaveMacroState(PlayerMacroState macro)
        {
            if (macro == null)
                throw new ArgumentNullException("macro");

            var filename = Path.Combine("saves", string.Format("{0}.xml", macro.Client.Name.Trim()));
            var state = new SavedMacroState(macro);

            state.SaveToFile(filename);
        }

        void LoadMacroState(Player player)
        {
            if (player == null)
                throw new ArgumentNullException("player");

            if (!Directory.Exists("saves"))
                return;

            var filename = Path.Combine("saves", string.Format("{0}.xml", player.Name));

            if (!File.Exists(filename))
                return;

            var macroState = SavedMacroState.LoadFromFile(filename);

            if (macroState == null)
                return;

            MacroManager.Instance.ImportMacroState(player, macroState);

            if (File.Exists(filename))
                File.Delete(filename);
        }

        #region Toolbar Button Click Methods
        void startNewClientButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchClient();
        }

        void startMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null || selectedMacro.Client == null || !selectedMacro.Client.IsLoggedIn)
                return;

            selectedMacro.Client.Update(PlayerFieldFlags.Location);
            var location = selectedMacro.Client.Location;

            selectedMacro.Start();
        }

        void pauseMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.Pause();
        }

        void stopMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.Stop();
        }

        void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsWindow();
        }
        #endregion

        void clientListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;
            if (listBoxItem == null)
                return;

            var player = listBoxItem.Content as Player;
            if (player == null)
                return;

            NativeMethods.SetForegroundWindow(player.Process.WindowHandle);
        }

        void spellQueueListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;
            if (listBoxItem == null)
                return;

            var queueItem = listBoxItem.Content as SpellQueueItem;
            if (queueItem == null)
                return;

            if (selectedMacro == null)
                return;

            var player = selectedMacro.Client;
            var spell = player.Spellbook.GetSpell(queueItem.Name);

            if (spell == null)
                return;

            var dialog = new SpellTargetWindow(spell, queueItem);
            dialog.Owner = this;

            var result = dialog.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            dialog.SpellQueueItem.CopyTo(queueItem);
        }

        void spellQueueListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;
            if (listBoxItem == null)
                return;

            var spell = listBoxItem.Content as SpellQueueItem;
            if (spell == null)
                return;

            if (selectedMacro == null)
                return;

            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                if (selectedMacro.RemoveFromSpellQueue(spell))
                    RefreshSpellQueue();
            }
        }

        void spellQueueListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var draggedItem = sender as ListBoxItem;
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        void spellQueueListBox_Drop(object sender, DragEventArgs e)
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

        void flowerQueueListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;
            if (listBoxItem == null)
                return;

            var queueItem = listBoxItem.Content as FlowerQueueItem;
            if (queueItem == null)
                return;

            if (selectedMacro == null)
                return;

            var dialog = new FlowerTargetWindow(queueItem);
            dialog.Owner = this;

            var result = dialog.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            dialog.FlowerQueueItem.CopyTo(queueItem);
        }

        void flowerQueueListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;
            if (listBoxItem == null)
                return;

            var flower = listBoxItem.Content as FlowerQueueItem;
            if (flower == null)
                return;

            if (selectedMacro == null)
                return;

            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                if (selectedMacro.RemoveFromFlowerQueue(flower))
                    RefreshFlowerQueue();
            }
        }

        void flowerQueueListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var draggedItem = sender as ListBoxItem;
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        void flowerQueueListBox_Drop(object sender, DragEventArgs e)
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

        void clientListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null)
            {
                if (selectedMacro != null)
                    selectedMacro.PropertyChanged -= SelectedMacro_PropertyChanged;

                this.Title = "SleepHunter";
                selectedMacro = null;
                ToggleSpellQueue(false);
                ToggleSkills(false);
                ToggleSpells(false);
                ToggleFlower();
                return;
            }

            var player = listBox.SelectedItem as Player;
            if (player == null)
            {
                if (selectedMacro != null)
                    selectedMacro.PropertyChanged -= SelectedMacro_PropertyChanged;

                this.Title = "SleepHunter";
                selectedMacro = null;
                ToggleSpellQueue(false);
                ToggleSkills(false);
                ToggleSpells(false);
                ToggleFlower();
                return;
            }

            var macroState = MacroManager.Instance.GetMacroState(player);

            if (player.IsLoggedIn)
                this.Title = string.Format("SleepHunter - {0}", player.Name);
            else
                this.Title = "SleepHunter";

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

            UpdateUIForMacroStatus(selectedMacro.Status);

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

        void clientListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.None)
                return;

            var listBoxItem = sender as ListBoxItem;
            if (listBoxItem == null)
                return;

            var player = listBoxItem.Content as Player;
            if (player == null)
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
                        HotkeyManager.Instance.UnregisterHotkey(windowSource.Handle, player.Hotkey);

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

        void selectedMacro_SpellQueueChanged(object sender, SpellQueueItemEventArgs e)
        {
            var macro = sender as PlayerMacroState;
            if (macro == null)
                return;

            spellQueueListBox.ItemsSource = macro.QueuedSpells;
            RefreshSpellQueue();

            ToggleSpellQueue(tabControl.SelectedIndex == SpellsTabIndex && macro.TotalSpellsCount > 0);
        }

        void selectedMacro_FlowerQueueChanged(object sender, FlowerQueueItemEventArgs e)
        {
            var macro = sender as PlayerMacroState;
            if (macro == null)
                return;

            flowerListBox.ItemsSource = macro.FlowerTargets;
            RefreshFlowerQueue();
        }

        void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl == null)
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

        void TabDeselected(TabItem tab)
        {
            if (selectedMacro == null)
                return;
        }

        void TabSelected(TabItem tab)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.Client.SelectedTabIndex = tabControl.Items.IndexOf(tab);
            ToggleSpellQueue(selectedMacro.TotalSpellsCount > 0);
            ToggleFlower(selectedMacro.Client.HasLyliacPlant, selectedMacro.Client.HasLyliacVineyard);
        }

        void skillListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item == null)
                return;

            var skill = item.Content as Skill;
            if (skill == null)
                return;

            var player = clientListBox.SelectedItem as Player;
            if (player == null)
                return;

            if (skill.IsEmpty || string.IsNullOrWhiteSpace(skill.Name))
                return;

            player.Skillbook.ToggleActive(skill.Name);
        }

        void spellListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item == null)
                return;

            var spell = item.Content as Spell;
            if (spell == null)
                return;

            var player = clientListBox.SelectedItem as Player;
            if (player == null)
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

            var spellTargetWindow = new SpellTargetWindow(spell);
            spellTargetWindow.Owner = this;

            var result = spellTargetWindow.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            var queueItem = spellTargetWindow.SpellQueueItem;

            var isAlreadyQueued = selectedMacro.IsSpellInQueue(queueItem.Name);

            if (isAlreadyQueued && UserSettingsManager.Instance.Settings.WarnOnDuplicateSpells)
            {
                var okToAddAgain = this.ShowMessageBox("Already Queued",
                   string.Format("The spell '{0}' is already queued.\nDo you want to queue it again anyways?", spell.Name),
                   "This warning message can be disabled in the Spell Macro settings.",
                   MessageBoxButton.YesNo,
                   460, 240);

                if (!okToAddAgain.HasValue || !okToAddAgain.Value)
                    return;
            }

            selectedMacro.AddToSpellQueue(queueItem);
        }

        void removeSelectedSpellButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedSpell = spellQueueListBox.SelectedItem as SpellQueueItem;

            if (selectedMacro == null || selectedSpell == null)
                return;

            selectedMacro.RemoveFromSpellQueue(selectedSpell);
            RefreshSpellQueue();
        }

        void removeAllSpellsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.ClearSpellQueue();
            RefreshSpellQueue();
        }

        void addFlowerTargetButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            var flowerTargetDialog = new FlowerTargetWindow();
            flowerTargetDialog.Owner = this;

            var result = flowerTargetDialog.ShowDialog();
            if (!result.HasValue || !result.Value)
                return;

            var queueItem = flowerTargetDialog.FlowerQueueItem;
            queueItem.LastUsedTimestamp = DateTime.Now;
            selectedMacro.AddToFlowerQueue(queueItem);
        }

        void removeSelectedFlowerTargetButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedTarget = flowerListBox.SelectedItem as FlowerQueueItem;

            if (selectedMacro == null || selectedTarget == null)
                return;

            selectedMacro.RemoveFromFlowerQueue(selectedTarget);
            RefreshFlowerQueue();
        }


        void removeAllFlowerTargetsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.ClearFlowerQueue();
            RefreshFlowerQueue();
        }

        void UserSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UserSettings settings = sender as UserSettings;
            if (settings == null)
                return;

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

        void SelectedMacro_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var macro = sender as PlayerMacroState;
            if (macro == null)
                return;

            if (string.Equals("Status", e.PropertyName, StringComparison.OrdinalIgnoreCase))
                UpdateUIForMacroStatus(macro.Status);
        }

        void flowerVineyardCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            if (flowerVineyardCheckBox != null)
                selectedMacro.UseLyliacVineyard = flowerVineyardCheckBox.IsChecked.Value;
        }

        void flowerAlternateCharactersCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            if (flowerAlternateCharactersCheckBox != null)
                selectedMacro.FlowerAlternateCharacters = flowerAlternateCharactersCheckBox.IsChecked.Value;
        }

        void ToggleSkills(bool show = true)
        {
            temuairSkillListBox.Visibility = medeniaSkillListBox.Visibility = worldSkillListBox.Visibility = (show ? Visibility.Visible : Visibility.Collapsed);
            skillsTab.IsEnabled = show;
        }

        void ToggleSpells(bool show = true)
        {
            temuairSpellListBox.Visibility = medeniaSpellListBox.Visibility = worldSpellListBox.Visibility = (show ? Visibility.Visible : Visibility.Collapsed);
            spellsTab.IsEnabled = show;
        }

        void ToggleFlower(bool hasLyliacPlant = false, bool hasLyliacVineyard = false)
        {
            flowerTab.IsEnabled = hasLyliacPlant || hasLyliacVineyard;

            flowerAlternateCharactersCheckBox.IsEnabled = hasLyliacPlant;
            flowerVineyardCheckBox.IsEnabled = hasLyliacVineyard;

            if (!hasLyliacPlant)
                flowerAlternateCharactersCheckBox.IsChecked = false;

            if (!hasLyliacVineyard)
                flowerVineyardCheckBox.IsChecked = false;
        }

        public void CheckForUpdate(bool showWindow = true)
        {
            if (showWindow)
                this.ShowMessageBox("Not Implemented",
                   "Sorry, this feature is not currently implemented!",
                   "It should be implemented very soon.");
        }
    }
}
