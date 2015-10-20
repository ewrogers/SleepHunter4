using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

using SleepHunter.Data;
using SleepHunter.Metadata;

namespace SleepHunter
{
   public partial class MetadataEditorWindow : Window
   {
      public static readonly int SkillsTabIndex = 0;
      public static readonly int SpellsTabIndex = 1;
      public static readonly int StavesTabIndex = 2;
      public static readonly int WeaponsTabIndex = 3;
      public static readonly int ItemsTabIndex = 4;

      public int SelectedTabIndex
      {
         get { return (int)GetValue(SelectedTabIndexProperty); }
         set { SetValue(SelectedTabIndexProperty, value); }
      }

      public static readonly DependencyProperty SelectedTabIndexProperty =
          DependencyProperty.Register("SelectedTabIndex", typeof(int), typeof(MetadataEditorWindow), new PropertyMetadata(0));

      public MetadataEditorWindow()
      {
         InitializeComponent();
      }

      void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         var tabControl = sender as TabControl;
         if (tabControl == null) return;

         var tabItem = tabControl.SelectedItem as TabItem;
         if (tabItem == null)
            this.Title = "Metadata Editor";
         else
            this.Title = string.Format("Metadata Editor - {0}", (tabItem.Header as string).Replace("_", string.Empty));
      }

      void Window_Loaded(object sender, RoutedEventArgs e)
      {
         SkillMetadataManager.Instance.SkillAdded += OnSkillManagerUpdated;
         SkillMetadataManager.Instance.SkillChanged += OnSkillManagerUpdated;
         SkillMetadataManager.Instance.SkillRemoved += OnSkillManagerUpdated;

         SpellMetadataManager.Instance.SpellAdded += OnSpellManagerUpdated;
         SpellMetadataManager.Instance.SpellChanged += OnSpellManagerUpdated;
         SpellMetadataManager.Instance.SpellRemoved += OnSpellManagerUpdated;

         StaffMetadataManager.Instance.StaffAdded += OnStaffManagerUpdated;
         StaffMetadataManager.Instance.StaffUpdated += OnStaffManagerUpdated;
         StaffMetadataManager.Instance.StaffRemoved += OnStaffManagerUpdated;
      }
            
      void OnSkillManagerUpdated(object sender, SkillMetadataEventArgs e)
      {
         BindingOperations.GetBindingExpression(skillListView, ListView.ItemsSourceProperty).UpdateTarget();
      }

      void OnSpellManagerUpdated(object sender, SpellMetadataEventArgs e)
      {
         BindingOperations.GetBindingExpression(spellListView, ListView.ItemsSourceProperty).UpdateTarget();
      }

      void OnStaffManagerUpdated(object sender, StaffMetadataEventArgs e)
      {
         BindingOperations.GetBindingExpression(stavesListBox, ListBox.ItemsSourceProperty).UpdateTarget();
      }

      void Window_Closing(object sender, CancelEventArgs e)
      {
         SkillMetadataManager.Instance.SkillAdded -= OnSkillManagerUpdated;
         SkillMetadataManager.Instance.SkillChanged -= OnSkillManagerUpdated;
         SkillMetadataManager.Instance.SkillRemoved -= OnSkillManagerUpdated;

         SpellMetadataManager.Instance.SpellAdded -= OnSpellManagerUpdated;
         SpellMetadataManager.Instance.SpellChanged -= OnSpellManagerUpdated;
         SpellMetadataManager.Instance.SpellRemoved -= OnSpellManagerUpdated;

         StaffMetadataManager.Instance.StaffAdded -= OnStaffManagerUpdated;
         StaffMetadataManager.Instance.StaffUpdated -= OnStaffManagerUpdated;
         StaffMetadataManager.Instance.StaffRemoved -= OnStaffManagerUpdated;
      }

      #region Button Clicks
      void addButton_Click(object sender, RoutedEventArgs e)
      {
         if (tabControl.SelectedIndex == SkillsTabIndex)
            AddSkill();
         else if (tabControl.SelectedIndex == SpellsTabIndex)
            AddSpell();
         else if (tabControl.SelectedIndex == StavesTabIndex)
            AddStaff();
      }

      void editButton_Click(object sender, RoutedEventArgs e)
      {
         if (tabControl.SelectedIndex == SkillsTabIndex)
         {
            var skill = skillListView.SelectedItem as SkillMetadata;
            if (skill == null) return;

            EditSkill(skill);
         }
         else if (tabControl.SelectedIndex == SpellsTabIndex)
         {
            var spell = spellListView.SelectedItem as SpellMetadata;
            if (spell == null) return;

            EditSpell(spell);
         }
         else if (tabControl.SelectedIndex == StavesTabIndex)
         {
            var staff = stavesListBox.SelectedItem as StaffMetadata;
            if (staff == null) return;

            EditStaff(staff);
         }
      }

      void deleteButton_Click(object sender, RoutedEventArgs e)
      {
         var selectedNames = new List<string>();

         if (tabControl.SelectedIndex == SkillsTabIndex)
         {
            foreach (SkillMetadata skill in skillListView.SelectedItems)
               selectedNames.Add(skill.Name);

            foreach(var skillName in selectedNames)
               RemoveSkill(skillName);
         }
         else if (tabControl.SelectedIndex == SpellsTabIndex)
         {
            foreach (SpellMetadata spell in spellListView.SelectedItems)
               selectedNames.Add(spell.Name);

            foreach (var spellName in selectedNames)
               RemoveSpell(spellName);
         }
         else if (tabControl.SelectedIndex == StavesTabIndex)
         {
            foreach (StaffMetadata staff in stavesListBox.SelectedItems)
               selectedNames.Add(staff.Name);

            foreach (var staffName in selectedNames)
               RemoveStaff(staffName);
         }
      }

      void clearButton_Click(object sender, RoutedEventArgs e)
      {
         if (tabControl.SelectedIndex == SkillsTabIndex)
            ClearAllSkills();
         else if (tabControl.SelectedIndex == SpellsTabIndex)
            ClearAllSpells();
         else if (tabControl.SelectedIndex == StavesTabIndex)
            ClearAllStaves();
      }

      void saveButton_Click(object sender, RoutedEventArgs e)
      {
         if (tabControl.SelectedIndex == SkillsTabIndex)
            SaveSkills();
         else if (tabControl.SelectedIndex == SpellsTabIndex)
            SaveSpells();
         else if (tabControl.SelectedIndex == StavesTabIndex)
            SaveStaves();
      }

      void revertButton_Click(object sender, RoutedEventArgs e)
      {
         if (tabControl.SelectedIndex == SkillsTabIndex)
            ReloadSkills();
         else if (tabControl.SelectedIndex == SpellsTabIndex)
            ReloadSpells();
         else if (tabControl.SelectedIndex == StavesTabIndex)
            ReloadStaves();
      }
      #endregion

      #region Add Methods
      void AddSkill()
      {
         var skillWindow = new SkillEditorWindow();
         skillWindow.Owner = this;

         var result = skillWindow.ShowDialog();
         if (!result.HasValue || !result.Value) return;

         SkillMetadataManager.Instance.AddSkill(skillWindow.Skill);
         skillListView.SelectedItem = skillWindow.Skill;
         skillListView.ScrollIntoView(skillWindow.Skill);
      }

      void AddSpell()
      {
         var spellWindow = new SpellEditorWindow();
         spellWindow.Owner = this;

         var result = spellWindow.ShowDialog();
         if (!result.HasValue || !result.Value) return;

         SpellMetadataManager.Instance.AddSpell(spellWindow.Spell);
         spellListView.SelectedItem = spellWindow.Spell;
         spellListView.ScrollIntoView(spellWindow.Spell);
      }

      void AddStaff()
      {
         var staffWindow = new StaffEditorWindow();
         staffWindow.Owner = this;

         var result = staffWindow.ShowDialog();
         if (!result.HasValue || !result.Value) return;

         StaffMetadataManager.Instance.AddStaff(staffWindow.Staff);
         stavesListBox.SelectedItem = staffWindow.Staff;
         stavesListBox.ScrollIntoView(staffWindow.Staff);
      }

      void AddModifiers()
      {
         var staff = stavesListBox.SelectedItem as StaffMetadata;
         if (staff == null) return;

         var modifiersWindow = new LineModifiersEditorWindow();
         modifiersWindow.Owner = this;

         var result = modifiersWindow.ShowDialog();
         if (!result.HasValue || !result.Value) return;

         staff.AddModifiers(modifiersWindow.Modifiers);
         lineModifiersListBox.SelectedItem = modifiersWindow.Modifiers;
         lineModifiersListBox.ScrollIntoView(modifiersWindow.Modifiers);

         lineModifiersListBox.Items.Refresh();
      }
      #endregion

      #region Edit Methods
      void EditSkill(SkillMetadata skill)
      {
         var originalName = skill.Name;

         var skillWindow = new SkillEditorWindow(skill);
         skillWindow.Owner = this;

         var result = skillWindow.ShowDialog();
         if (!result.HasValue || !result.Value) return;

         skillWindow.Skill.CopyTo(skill);
         BindingOperations.GetBindingExpression(skillListView, ListView.ItemsSourceProperty).UpdateTarget();

         if (!string.Equals(skill.Name, originalName, StringComparison.Ordinal))
            SkillMetadataManager.Instance.RenameSkill(originalName, skill.Name);

         skillListView.SelectedItem = skill;
         skillListView.ScrollIntoView(skill);
      }

      void EditSpell(SpellMetadata spell)
      {
         var originalName = spell.Name;

         var spellWindow = new SpellEditorWindow(spell);
         spellWindow.Owner = this;

         var result = spellWindow.ShowDialog();
         if (!result.HasValue || !result.Value) return;

         spellWindow.Spell.CopyTo(spell);
         BindingOperations.GetBindingExpression(spellListView, ListView.ItemsSourceProperty).UpdateTarget();

         if (!string.Equals(spell.Name, originalName, StringComparison.Ordinal))
            SpellMetadataManager.Instance.RenameSpell(originalName, spell.Name);

         skillListView.SelectedItem = spell;
         skillListView.ScrollIntoView(spell);
      }

      void EditStaff(StaffMetadata staff)
      {
         var originalName = staff.Name;

         var staffWindow = new StaffEditorWindow(staff);
         staffWindow.Owner = this;

         var result = staffWindow.ShowDialog();
         if (!result.HasValue || !result.Value) return;

         staffWindow.Staff.CopyTo(staff, copyModifiers: false);
         BindingOperations.GetBindingExpression(stavesListBox, ListBox.ItemsSourceProperty).UpdateTarget();

         if (!string.Equals(staff.Name, originalName, StringComparison.Ordinal))
            StaffMetadataManager.Instance.RenameStaff(originalName, staff.Name);

         stavesListBox.SelectedItem = staff;
         stavesListBox.ScrollIntoView(staff);
      }

      void EditModifiers(StaffMetadata staff, SpellLineModifiers modifiers)
      {
         var modifiersWindow = new LineModifiersEditorWindow(modifiers);
         modifiersWindow.Owner = this;

         var result = modifiersWindow.ShowDialog();
         if (!result.HasValue || !result.Value) return;

         staff.ChangeModifiers(modifiers, modifiersWindow.Modifiers);
         lineModifiersListBox.Items.Refresh();

         lineModifiersListBox.SelectedItem = modifiers;
         lineModifiersListBox.ScrollIntoView(modifiers);
      }
      #endregion

      #region Remove Methods
      bool RemoveSkill(string skillName)
      {
         return SkillMetadataManager.Instance.RemoveSkill(skillName);
      }

      bool RemoveSpell(string spellName)
      {
         return SpellMetadataManager.Instance.RemoveSpell(spellName);
      }

      bool RemoveStaff(string staffName)
      {
         return StaffMetadataManager.Instance.RemoveStaff(staffName);
      }
      
      bool RemoveModifiers(SpellLineModifiers modifiers)
      {
         var staff = stavesListBox.SelectedItem as StaffMetadata;
         if (staff == null) return false;

         var wasRemoved = staff.RemoveModifiers(modifiers);

         if (wasRemoved)
            lineModifiersListBox.Items.Refresh();

         return wasRemoved;
      }
      #endregion

      #region Clear Methods
      void ClearAllSkills()
      {
         var isOkayToClear = this.ShowMessageBox("Clear All Skills",
            "This will remove every skill from the list.\nAre you sure?",
            "This action cannot be undone.",
            MessageBoxButton.YesNo);

         if (isOkayToClear.HasValue && isOkayToClear.Value)
         {
            SkillMetadataManager.Instance.ClearSkills();
            BindingOperations.GetBindingExpression(skillListView, ListView.ItemsSourceProperty).UpdateTarget();
         }
      }

      void ClearAllSpells()
      {
         var isOkayToClear = this.ShowMessageBox("Clear All Spells",
            "This will remove every spell from the list.\nAre you sure?",
            "This action cannot be undone.",
            MessageBoxButton.YesNo);

         if (isOkayToClear.HasValue && isOkayToClear.Value)
         {
            SpellMetadataManager.Instance.ClearSpells();
            BindingOperations.GetBindingExpression(spellListView, ListView.ItemsSourceProperty).UpdateTarget();
         }
      }

      void ClearAllStaves()
      {
         var isOkayToClear = this.ShowMessageBox("Clear All Staves",
            "This will remove every staff from the list.\nAre you sure?",
            "This action cannot be undone.",
            MessageBoxButton.YesNo);

         if (isOkayToClear.HasValue && isOkayToClear.Value)
         {
            StaffMetadataManager.Instance.ClearStaves();
            BindingOperations.GetBindingExpression(stavesListBox, ListBox.ItemsSourceProperty).UpdateTarget();
         }
      }
      #endregion

      #region Save Methods
      void SaveSkills()
      {
         try
         {
            SkillMetadataManager.Instance.SaveToFile(SkillMetadataManager.SkillMetadataFile);
         }
         catch(Exception ex)
         {
            this.ShowMessageBox("Error Saving",
               string.Format("Unable to save skills metadata:\n{0}", ex.Message),
               "Check file permissions and that it is not marked as read-only.",
               MessageBoxButton.OK,
               440, 260);
         }
      }

      void SaveSpells()
      {
         try
         {
            SpellMetadataManager.Instance.SaveToFile(SpellMetadataManager.SpellMetadataFile);
         }
         catch (Exception ex)
         {
            this.ShowMessageBox("Error Saving",
               string.Format("Unable to save spells metadata:\n{0}", ex.Message),
               "Check file permissions and that it is not marked as read-only.",
               MessageBoxButton.OK,
               440, 260);
         }
      }

      void SaveStaves()
      {
         try
         {
            StaffMetadataManager.Instance.SaveToFile(StaffMetadataManager.StaffMetadataFile);
         }
         catch (Exception ex)
         {
            this.ShowMessageBox("Error Saving",
               string.Format("Unable to save staves metadata:\n{0}", ex.Message),
               "Check file permissions and that it is not marked as read-only.",
               MessageBoxButton.OK,
               440, 260);
         }
      }
      #endregion

      #region Reload Methods
      void ReloadSkills()
      {
         try
         {
            var okToReload = this.ShowMessageBox("Reload Skills",
               "Reloading skill metadata from file will lose all unsaved changes.\nDo you wish to continue?",
               "This operation cannot be undone.",
               MessageBoxButton.YesNo,
               520, 240);

            if (!okToReload.HasValue || !okToReload.Value) return;

            SkillMetadataManager.Instance.ClearSkills();
            SkillMetadataManager.Instance.LoadFromFile(SkillMetadataManager.SkillMetadataFile);
         }
         catch (Exception ex)
         {
            this.ShowMessageBox("Error Loading",
               string.Format("Unable to load skills metadata:\n{0}", ex.Message),
               "Check file permissions and that it still exists.",
               MessageBoxButton.OK,
               440, 260);
         }
      }

      void ReloadSpells()
      {
         try
         {
            var okToReload = this.ShowMessageBox("Reload Spells",
               "Reloading spell metadata from file will lose all unsaved changes.\nDo you wish to continue?",
               "This operation cannot be undone.",
               MessageBoxButton.YesNo,
               520, 240);

            if (!okToReload.HasValue || !okToReload.Value) return;

            SpellMetadataManager.Instance.ClearSpells();
            SpellMetadataManager.Instance.LoadFromFile(SpellMetadataManager.SpellMetadataFile);
         }
         catch (Exception ex)
         {
            this.ShowMessageBox("Error Loading",
               string.Format("Unable to load spells metadata:\n{0}", ex.Message),
               "Check file permissions and that it still exists.",
               MessageBoxButton.OK,
               440, 260);
         }
      }

      void ReloadStaves()
      {
         try
         {
            var okToReload = this.ShowMessageBox("Reload Staves",
               "Reloading staff metadata from file will lose all unsaved changes.\nDo you wish to continue?",
               "This operation cannot be undone.",
               MessageBoxButton.YesNo,
               520, 240);

            if (!okToReload.HasValue || !okToReload.Value) return;

            StaffMetadataManager.Instance.ClearStaves();
            StaffMetadataManager.Instance.LoadFromFile(StaffMetadataManager.StaffMetadataFile);
         }
         catch (Exception ex)
         {
            this.ShowMessageBox("Error Loading",
               string.Format("Unable to save staff metadata:\n{0}", ex.Message),
               "Check file permissions and that it still exists.",
               MessageBoxButton.OK,
               440, 260);
         }
      }
      #endregion

      void skillSpellListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         var listView = sender as ListView;
         if (listView == null) return;

         int selectedCount = listView.SelectedItems.Count;

         editButton.IsEnabled = selectedCount == 1;
         removeButton.IsEnabled = selectedCount > 0;
         clearButton.IsEnabled = selectedCount > 0;
      }

      void stavesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         var listBox = sender as ListBox;
         if (listBox == null)
         {
            addModifierButton.Visibility = editModifierButton.Visibility = removeModifierButton.Visibility = Visibility.Collapsed;
            return;
         }

         if (e.AddedItems.Count < 1)
         {
            addModifierButton.Visibility = editModifierButton.Visibility = removeModifierButton.Visibility = Visibility.Collapsed;
            lineModifiersListBox.ItemsSource = null;
            return;
         }

         var staff = e.AddedItems[0] as StaffMetadata;
         if (staff == null)
         {
            addModifierButton.Visibility = editModifierButton.Visibility = removeModifierButton.Visibility = Visibility.Collapsed;
            return;
         }

         addModifierButton.Visibility = editModifierButton.Visibility = removeModifierButton.Visibility = Visibility.Visible;
         lineModifiersListBox.ItemsSource = staff.Modifiers;

         if (staff.Modifiers.Count > 0)
            lineModifiersListBox.SelectedIndex = 0;
      }

      void skillListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
      {
         var item = sender as ListViewItem;
         if (item == null) return;

         var skill = item.Content as SkillMetadata;
         if (skill == null) return;

         EditSkill(skill);
      }

      void spellListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
      {
         var item = sender as ListViewItem;
         if (item == null) return;

         var spell = item.Content as SpellMetadata;
         if (spell == null) return;

         EditSpell(spell);
      }

      void stavesListBoxItem_DoubleClick(object sender, MouseButtonEventArgs e)
      {
         var item = sender as ListBoxItem;
         if (item == null) return;

         var staff = item.Content as StaffMetadata;
         if (staff == null) return;

         EditStaff(staff);
      }

      void lineModifiersListBoxItem_DoubleClick(object sender, MouseButtonEventArgs e)
      {
         var item = sender as ListBoxItem;
         if (item == null) return;

         var staff = stavesListBox.SelectedItem as StaffMetadata;
         if (staff == null) return;

         var modifiers = item.Content as SpellLineModifiers;
         if (modifiers == null) return;

         EditModifiers(staff, modifiers);
      }

      void addModifierButton_Click(object sender, RoutedEventArgs e)
      {
         var staff = stavesListBox.SelectedItem as StaffMetadata;
         if (staff == null) return;

         AddModifiers();
      }

      void editModifierButton_Click(object sender, RoutedEventArgs e)
      {
         var staff = stavesListBox.SelectedItem as StaffMetadata;
         if (staff == null) return;

         var modifiers = lineModifiersListBox.SelectedItem as SpellLineModifiers;
         if (modifiers == null) return;

         EditModifiers(staff, modifiers);
      }

      void removeModifierButton_Click(object sender, RoutedEventArgs e)
      {
         var staff = stavesListBox.SelectedItem as StaffMetadata;
         if (staff == null) return;

         var modifiers = lineModifiersListBox.SelectedItem as SpellLineModifiers;
         if (modifiers == null) return;

         RemoveModifiers(modifiers);
      }
   }
}
