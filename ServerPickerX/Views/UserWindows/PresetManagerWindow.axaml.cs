using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ServerPickerX.Models;
using ServerPickerX.Services.DependencyInjection;
using ServerPickerX.Services.Localizations;
using ServerPickerX.Services.MessageBoxes;
using ServerPickerX.Settings;
using ServerPickerX.ViewModels;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ServerPickerX.Views
{
    public partial class PresetManagerWindow : Window
    {
        // Triangles drawn beside the sorted column header
        private static readonly Geometry SortAscendingGlyph = StreamGeometry.Parse("M0,4 L4,0 L8,4 Z");
        private static readonly Geometry SortDescendingGlyph = StreamGeometry.Parse("M0,0 L4,4 L8,0 Z");

        private bool _committingPresetName;
        private bool _isEditingPresetName;
        private string? _editingPresetOriginalName;
        private PresetModel? _editingPreset;
        private TextBox? _presetNameEditor;
        private TextBlock? _presetNameLabel;
        private ListSortDirection? _presetSortDirection;
        private string? _serverSortColumn;
        private ListSortDirection _serverSortDirection = ListSortDirection.Ascending;

        private readonly IMessageBoxService _messageBoxService;
        private readonly ILocalizationService _localizationService;

        public PresetManagerWindow()
        {
            InitializeComponent();
            HookListKeys();

            _messageBoxService = ServiceLocator.GetRequiredService<IMessageBoxService>();
            _localizationService = ServiceLocator.GetRequiredService<ILocalizationService>();
        }

        public PresetManagerWindow(MainWindowViewModel mainVm)
        {
            InitializeComponent();
            HookListKeys();

            _messageBoxService = ServiceLocator.GetRequiredService<IMessageBoxService>();
            _localizationService = ServiceLocator.GetRequiredService<ILocalizationService>();

            DataContext = new PresetManagerWindowViewModel(
                mainVm,
                ServiceLocator.GetRequiredService<JsonSetting>(),
                ServiceLocator.GetRequiredService<IMessageBoxService>(),
                ServiceLocator.GetRequiredService<ILocalizationService>()
                );
        }

        // The keys are caught on the way down, before the list's own handling, which
        // would otherwise use Space to change the selection
        private void HookListKeys()
        {
            PresetList.AddHandler(KeyDownEvent, PresetList_KeyDown, RoutingStrategies.Tunnel);
            ServerList.AddHandler(KeyDownEvent, ServerList_KeyDown, RoutingStrategies.Tunnel);
        }

        private async void ShareBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            string? code = vm.ExportSelectedPresetCode();

            if (string.IsNullOrEmpty(code))
            {
                return;
            }

            IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

            if (clipboard == null)
            {
                return;
            }

            await clipboard.SetTextAsync(code);

            await _messageBoxService.ShowMessageBoxAsync(
                _localizationService.GetLocaleValue("MessageBoxInfoTitle"),
                _localizationService.GetLocaleValue("PresetCodeCopiedDialogue")
                );
        }

        // The code arrives by clipboard rather than a text field, which is how it
        // reaches the user in the first place
        private async void ImportBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

            if (clipboard == null)
            {
                return;
            }

            string? code = await clipboard.TryGetTextAsync();

            PresetImportResult result = await vm.ImportPresetFromCodeAsync(code);

            string message = result switch
            {
                PresetImportResult.Imported => _localizationService.GetLocaleValue("PresetImportedDialogue"),
                PresetImportResult.WrongGameMode => _localizationService.GetLocaleValue("PresetImportWrongGameDialogue"),
                _ => _localizationService.GetLocaleValue("PresetImportInvalidDialogue"),
            };

            await _messageBoxService.ShowMessageBoxAsync(
                _localizationService.GetLocaleValue("MessageBoxInfoTitle"),
                message
                );

            if (result == PresetImportResult.Imported)
            {
                ReapplyPresetSortIfNeeded();
                ReapplyServerSortIfNeeded();
            }
        }

        private async void AddBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            await vm.AddPresetAsync();
            ReapplyPresetSortIfNeeded();
            ReapplyServerSortIfNeeded();
            BeginEditingSelectedPreset();
        }

        private async void DeleteBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            bool deleted = await vm.DeletePresetsAsync(GetSelectedPresetItems());

            if (deleted)
            {
                ReapplyPresetSortIfNeeded();
                ReapplyServerSortIfNeeded();
                RestorePresetListFocus();
            }
        }

        private async void ApplyBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            if (await vm.ApplySelectedPresetAsync())
            {
                Close();
            }
        }

        private async void ClusterToggleBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            await vm.ToggleSelectedPresetClusterModeAsync();
            ReapplyServerSortIfNeeded();
        }

        private void PresetList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            ReapplyServerSortIfNeeded();
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            e.Handled = true;
            (TopLevel.GetTopLevel(this) as Window)?.BeginMoveDrag(e);
        }

        private async Task CommitPresetNameAsync(PresetModel presetItem, string originalPresetName)
        {
            if (_committingPresetName || DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            _committingPresetName = true;

            try
            {
                await vm.RenamePresetAsync(presetItem, originalPresetName);
                ReapplyPresetSortIfNeeded();
                ReapplyServerSortIfNeeded();
            }
            finally
            {
                _committingPresetName = false;
                _editingPresetOriginalName = null;
            }
        }

        private void PresetList_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_isEditingPresetName || DataContext is not PresetManagerWindowViewModel vm || vm.SelectedPresetItem == null)
            {
                return;
            }

            // Only a double click on a row renames, not one on empty space below the rows
            if (e.Source is not Visual sourceVisual || sourceVisual.FindAncestorOfType<ListBoxItem>(true) == null)
            {
                return;
            }

            BeginEditingSelectedPreset();
        }

        private async void PresetList_KeyDown(object? sender, KeyEventArgs e)
        {
            // Keys typed into the rename box belong to the rename box
            if (_isEditingPresetName || DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            if (e.Key == Key.Delete)
            {
                List<PresetModel> selectedPresetItems = GetSelectedPresetItems();

                if (selectedPresetItems.Count == 0)
                {
                    return;
                }

                e.Handled = true;
                bool deleted = await vm.DeletePresetsAsync(selectedPresetItems);

                if (deleted)
                {
                    RestorePresetListFocus();
                }

                return;
            }

            if (e.Key == Key.F2)
            {
                e.Handled = true;
                BeginEditingSelectedPreset();
            }
        }

        private void PresetHeader_Tapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            _presetSortDirection = _presetSortDirection switch
            {
                ListSortDirection.Ascending => ListSortDirection.Descending,
                _ => ListSortDirection.Ascending,
            };

            vm.SortPresets(_presetSortDirection.Value);
            UpdatePresetSortGlyph();
        }

        private async void AllowedCheckBox_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            await vm.PersistSelectedPresetServerKeysAsync();
            ReapplyServerSortIfNeeded();
        }

        private async void AllowAllBtn_Click(object? sender, RoutedEventArgs e)
        {
            await SetAllPresetServersAllowedAsync(true);
        }

        private async void AllowNoneBtn_Click(object? sender, RoutedEventArgs e)
        {
            await SetAllPresetServersAllowedAsync(false);
        }

        private async Task SetAllPresetServersAllowedAsync(bool isAllowed)
        {
            if (DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            await vm.SetAllPresetServersAllowedAsync(isAllowed);
            ReapplyServerSortIfNeeded();
        }

        private async void ServerList_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Space || DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            // A focused checkbox toggles itself, the rest of the selection is left alone
            if (e.Source is Visual sourceVisual && sourceVisual.FindAncestorOfType<CheckBox>(true) != null)
            {
                return;
            }

            List<PresetServerModel> selectedItems = (ServerList.SelectedItems ?? System.Array.Empty<object>())
                .OfType<PresetServerModel>()
                .ToList();

            if (selectedItems.Count == 0)
            {
                return;
            }

            e.Handled = true;

            bool shouldAllow = selectedItems.Any(serverItem => !serverItem.IsAllowed);

            foreach (PresetServerModel serverItem in selectedItems)
            {
                serverItem.IsAllowed = shouldAllow;
            }

            await vm.PersistSelectedPresetServerKeysAsync();
            ReapplyServerSortIfNeeded();
        }

        private void ServerHeader_Tapped(object? sender, TappedEventArgs e)
        {
            if (sender is not Control { Tag: string sortKey })
            {
                return;
            }

            // Allowed servers come first on the first click, everything else sorts A to Z
            ListSortDirection defaultDirection = sortKey == "Allowed"
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            SortServerItems(sortKey, defaultDirection);
            UpdateServerSortGlyphs();
        }

        private void SortServerItems(string columnName, ListSortDirection defaultDirection = ListSortDirection.Ascending)
        {
            if (DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            if (_serverSortColumn == columnName)
            {
                _serverSortDirection = _serverSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                _serverSortColumn = columnName;
                _serverSortDirection = defaultDirection;
            }

            vm.SortServerItems(columnName, _serverSortDirection);
        }

        private void ReapplyPresetSortIfNeeded()
        {
            if (_presetSortDirection == null || DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            vm.SortPresets(_presetSortDirection.Value);
        }

        private void ReapplyServerSortIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(_serverSortColumn) || DataContext is not PresetManagerWindowViewModel vm)
            {
                return;
            }

            vm.SortServerItems(_serverSortColumn, _serverSortDirection);
        }

        private void UpdatePresetSortGlyph()
        {
            PresetSortGlyph.IsVisible = _presetSortDirection != null;
            PresetSortGlyph.Data = _presetSortDirection == ListSortDirection.Descending
                ? SortDescendingGlyph
                : SortAscendingGlyph;
        }

        private void UpdateServerSortGlyphs()
        {
            (string SortKey, Path Glyph)[] columns =
            [
                ("Allowed", AllowedSortGlyph),
                ("Flag", FlagSortGlyph),
                ("ServerId", IdSortGlyph),
                ("ServerName", LocationSortGlyph),
            ];

            foreach ((string sortKey, Path glyph) in columns)
            {
                glyph.IsVisible = sortKey == _serverSortColumn;
                glyph.Data = _serverSortDirection == ListSortDirection.Descending
                    ? SortDescendingGlyph
                    : SortAscendingGlyph;
            }
        }

        private void RestorePresetListFocus()
        {
            Dispatcher.UIThread.Post(() => PresetList.Focus());
        }

        // Renaming swaps the row's text for a text box in place. Enter or clicking away
        // saves, Esc cancels, the same as the grid this replaced
        private void BeginEditingSelectedPreset()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_isEditingPresetName
                    || DataContext is not PresetManagerWindowViewModel vm
                    || vm.SelectedPresetItem == null
                    || GetSelectedPresetItems().Count != 1)
                {
                    return;
                }

                PresetModel preset = vm.SelectedPresetItem;

                PresetList.ScrollIntoView(preset);

                // A row scrolled into view only exists after the next layout pass
                Dispatcher.UIThread.Post(() => StartEditingPresetName(preset), DispatcherPriority.Background);
            }, DispatcherPriority.Background);
        }

        private void StartEditingPresetName(PresetModel preset)
        {
            if (_isEditingPresetName || PresetList.ContainerFromItem(preset) is not Control row)
            {
                return;
            }

            TextBox? editor = row.GetVisualDescendants()
                .OfType<TextBox>()
                .FirstOrDefault(textBox => textBox.Name == "PresetNameEditor");
            TextBlock? label = row.GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(textBlock => textBlock.Name == "PresetNameText");

            if (editor == null || label == null)
            {
                return;
            }

            _isEditingPresetName = true;
            _editingPreset = preset;
            _editingPresetOriginalName = preset.Name;
            _presetNameEditor = editor;
            _presetNameLabel = label;

            editor.Text = preset.Name;
            editor.KeyDown += PresetNameEditor_KeyDown;
            editor.LostFocus += PresetNameEditor_LostFocus;

            label.IsVisible = false;
            editor.IsVisible = true;
            editor.Focus();
            editor.SelectAll();
        }

        private async void PresetNameEditor_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await FinishEditingPresetNameAsync(true);
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                await FinishEditingPresetNameAsync(false);
            }
        }

        private async void PresetNameEditor_LostFocus(object? sender, RoutedEventArgs e)
        {
            await FinishEditingPresetNameAsync(true);
        }

        private async Task FinishEditingPresetNameAsync(bool shouldCommit)
        {
            if (!_isEditingPresetName)
            {
                return;
            }

            _isEditingPresetName = false;

            PresetModel? preset = _editingPreset;
            string originalPresetName = _editingPresetOriginalName ?? preset?.Name ?? string.Empty;
            string editedPresetName = _presetNameEditor?.Text ?? string.Empty;

            // Unhooked before hiding, hiding a focused box would fire LostFocus again
            if (_presetNameEditor != null)
            {
                _presetNameEditor.KeyDown -= PresetNameEditor_KeyDown;
                _presetNameEditor.LostFocus -= PresetNameEditor_LostFocus;
                _presetNameEditor.IsVisible = false;
            }

            if (_presetNameLabel != null)
            {
                _presetNameLabel.IsVisible = true;
            }

            _editingPreset = null;
            _presetNameEditor = null;
            _presetNameLabel = null;

            if (!shouldCommit || preset == null)
            {
                _editingPresetOriginalName = null;
                RestorePresetListFocus();

                return;
            }

            preset.Name = editedPresetName;

            await CommitPresetNameAsync(preset, originalPresetName);

            RestorePresetListFocus();
        }

        private List<PresetModel> GetSelectedPresetItems()
        {
            IEnumerable selectedItems = PresetList.SelectedItems ?? System.Array.Empty<object>();
            List<PresetModel> selectedPresetItems = selectedItems
                .OfType<PresetModel>()
                .Distinct()
                .ToList();

            if (selectedPresetItems.Count == 0 && DataContext is PresetManagerWindowViewModel vm && vm.SelectedPresetItem != null)
            {
                selectedPresetItems.Add(vm.SelectedPresetItem);
            }

            return selectedPresetItems;
        }
    }
}
