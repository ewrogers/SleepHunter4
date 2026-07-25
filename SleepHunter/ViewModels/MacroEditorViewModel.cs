using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SleepHunter.Macro;
using SleepHunter.Models;

namespace SleepHunter.ViewModels
{
    public sealed partial class MacroEditorViewModel :
        ObservableObject,
        IDisposable
    {
        private readonly Func<bool> canEdit;
        private readonly PlayerMacroConfiguration configuration;
        private bool isDisposed;

        public MacroEditorViewModel(
            PlayerMacroConfiguration configuration,
            Func<bool> canEdit = null)
        {
            this.configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            this.canEdit = canEdit ?? (() => true);

            ((INotifyCollectionChanged)configuration.QueuedSpells)
                .CollectionChanged += OnSpellsChanged;
            ((INotifyCollectionChanged)configuration.FlowerTargets)
                .CollectionChanged += OnFlowersChanged;
        }

        public PlayerMacroConfiguration Configuration =>
            configuration;

        public ReadOnlyObservableCollection<SpellQueueItem>
            QueuedSpells => configuration.QueuedSpells;

        public ReadOnlyObservableCollection<FlowerQueueItem>
            FlowerTargets => configuration.FlowerTargets;

        public bool HasSpells => QueuedSpells.Count > 0;

        public bool HasFlowers => FlowerTargets.Count > 0;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveSelectedSpellCommand))]
        public partial SpellQueueItem SelectedSpell { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveSelectedFlowerCommand))]
        public partial FlowerQueueItem SelectedFlower { get; set; }

        public void Dispose()
        {
            if (isDisposed)
                return;

            ((INotifyCollectionChanged)configuration.QueuedSpells)
                .CollectionChanged -= OnSpellsChanged;
            ((INotifyCollectionChanged)configuration.FlowerTargets)
                .CollectionChanged -= OnFlowersChanged;
            isDisposed = true;
            SelectedSpell = null;
            SelectedFlower = null;
            NotifyEditingStateChanged();
        }

        public void NotifyEditingStateChanged()
        {
            RemoveSelectedSpellCommand.NotifyCanExecuteChanged();
            ClearSpellsCommand.NotifyCanExecuteChanged();
            RemoveSelectedFlowerCommand.NotifyCanExecuteChanged();
            ClearFlowersCommand.NotifyCanExecuteChanged();
        }

        public bool MoveSpell(
            SpellQueueItem spell,
            SpellQueueItem target)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            return canEdit() &&
                   configuration.MoveSpell(spell, target);
        }

        public bool MoveFlower(
            FlowerQueueItem flower,
            FlowerQueueItem target)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            return canEdit() &&
                   configuration.MoveFlower(flower, target);
        }

        [RelayCommand(CanExecute = nameof(CanRemoveSelectedSpell))]
        private void RemoveSelectedSpell()
        {
            if (!CanRemoveSelectedSpell())
                return;

            var selected = SelectedSpell;
            if (configuration.RemoveFromSpellQueue(selected))
                SelectedSpell = null;
        }

        private bool CanRemoveSelectedSpell() =>
            !isDisposed &&
            canEdit() &&
            SelectedSpell is not null &&
            QueuedSpells.Contains(SelectedSpell);

        [RelayCommand(CanExecute = nameof(CanClearSpells))]
        private void ClearSpells()
        {
            if (!CanClearSpells())
                return;

            configuration.ClearSpellQueue();
            SelectedSpell = null;
        }

        private bool CanClearSpells() =>
            !isDisposed &&
            canEdit() &&
            HasSpells;

        [RelayCommand(CanExecute = nameof(CanRemoveSelectedFlower))]
        private void RemoveSelectedFlower()
        {
            if (!CanRemoveSelectedFlower())
                return;

            var selected = SelectedFlower;
            if (configuration.RemoveFromFlowerQueue(selected))
                SelectedFlower = null;
        }

        private bool CanRemoveSelectedFlower() =>
            !isDisposed &&
            canEdit() &&
            SelectedFlower is not null &&
            FlowerTargets.Contains(SelectedFlower);

        [RelayCommand(CanExecute = nameof(CanClearFlowers))]
        private void ClearFlowers()
        {
            if (!CanClearFlowers())
                return;

            configuration.ClearFlowerQueue();
            SelectedFlower = null;
        }

        private bool CanClearFlowers() =>
            !isDisposed &&
            canEdit() &&
            HasFlowers;

        private void OnSpellsChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (SelectedSpell is not null &&
                !QueuedSpells.Contains(SelectedSpell))
            {
                SelectedSpell = null;
            }

            OnPropertyChanged(nameof(HasSpells));
            ClearSpellsCommand.NotifyCanExecuteChanged();
            RemoveSelectedSpellCommand.NotifyCanExecuteChanged();
        }

        private void OnFlowersChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (SelectedFlower is not null &&
                !FlowerTargets.Contains(SelectedFlower))
            {
                SelectedFlower = null;
            }

            OnPropertyChanged(nameof(HasFlowers));
            ClearFlowersCommand.NotifyCanExecuteChanged();
            RemoveSelectedFlowerCommand.NotifyCanExecuteChanged();
        }
    }
}
