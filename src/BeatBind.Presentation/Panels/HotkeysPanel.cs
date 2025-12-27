using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Presentation.Components;
using BeatBind.Presentation.Helpers;
using BeatBind.Presentation.Themes;
using MaterialSkin.Controls;
using Microsoft.Extensions.Logging;

namespace BeatBind.Presentation.Panels;

public partial class HotkeysPanel : BasePanelControl
{
    private readonly HotkeyApplicationService? _hotkeyApplicationService;

    private MaterialLabel _lastHotkeyLabel = null!;
    private FlowLayoutPanel _hotkeyFlowPanel = null!;
    private MaterialButton _addHotkeyButton = null!;
    private readonly Dictionary<string, HotkeyListItem> _hotkeyEntries = new();
    private List<Hotkey> _originalHotkeys = new();

    public event EventHandler<Hotkey>? HotkeyEditRequested;
    public event EventHandler<Hotkey>? HotkeyDeleteRequested;
    public event EventHandler? HotkeyAdded;
    public event EventHandler? ConfigurationChanged;

    /// <summary>
    /// Initializes a new instance of the HotkeysPanel with dependency injection.
    /// </summary>
    /// <param name="hotkeyApplicationService">Service for hotkey management operations</param>
    /// <param name="logger">Logger instance</param>
    public HotkeysPanel(HotkeyApplicationService? hotkeyApplicationService, ILogger<HotkeysPanel> logger)
        : base(logger)
    {
        _hotkeyApplicationService = hotkeyApplicationService;
    }

    /// <summary>
    /// Parameterless constructor for WinForms designer support.
    /// </summary>
    public HotkeysPanel() : base()
    {
        _hotkeyApplicationService = null;
    }

    /// <summary>
    /// Initializes the base component settings for the panel.
    /// </summary>
    protected override void InitializeComponent()
    {
        SuspendLayout();

        Dock = DockStyle.Fill;
        BackColor = Theme.CardBackground;
        Padding = new Padding(20);

        ResumeLayout(false);
    }

    /// <summary>
    /// Initializes the UI layout and controls for the hotkeys panel.
    /// </summary>
    protected override void InitializeUI()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.CardBackground
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Use CardFactory for consistent card creation
        var lastHotkeyCard = CardFactory.CreateModernCard("Last Triggered Hotkey", CreateLastHotkeyContent());
        mainLayout.Controls.Add(lastHotkeyCard);

        var hotkeyCard = CardFactory.CreateModernCard("Hotkey Management", CreateHotkeyManagementContent());
        mainLayout.Controls.Add(hotkeyCard);

        Controls.Add(mainLayout);
    }

    /// <summary>
    /// Creates the last triggered hotkey display section.
    /// </summary>
    /// <returns>A control showing the most recently triggered hotkey</returns>
    private Control CreateLastHotkeyContent()
    {
        var panel = new Panel { Height = 30, Dock = DockStyle.Top };

        // Use ControlFactory for consistent control creation
        _lastHotkeyLabel = ControlFactory.CreateMaterialLabel("No hotkey triggered yet", highEmphasis: false);

        panel.Controls.Add(_lastHotkeyLabel);
        return panel;
    }

    /// <summary>
    /// Creates the hotkey management section with add button and hotkey list.
    /// </summary>
    /// <returns>A control containing hotkey management UI</returns>
    private Control CreateHotkeyManagementContent()
    {
        var panel = new Panel { Dock = DockStyle.Fill };

        // Use ControlFactory for consistent button creation
        _addHotkeyButton = ControlFactory.CreateMaterialButton("ADD NEW HOTKEY", 180, 40);
        _addHotkeyButton.Dock = DockStyle.Top;
        _addHotkeyButton.Margin = new Padding(0, 0, 0, 10);
        _addHotkeyButton.Location = new Point(0, 8);
        _addHotkeyButton.Click += AddHotkeyButton_Click;

        _hotkeyFlowPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Theme.CardBackground,
            Padding = new Padding(0, 5, 5, 5)
        };
        _hotkeyFlowPanel.HorizontalScroll.Enabled = false;
        _hotkeyFlowPanel.HorizontalScroll.Visible = false;

        _hotkeyFlowPanel.ClientSizeChanged += (s, e) =>
        {
            _hotkeyFlowPanel.SuspendLayout();
            var scrollbarWidth = _hotkeyFlowPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            var availableWidth = _hotkeyFlowPanel.ClientSize.Width - _hotkeyFlowPanel.Padding.Horizontal - scrollbarWidth - 5;

            foreach (Control control in _hotkeyFlowPanel.Controls)
            {
                if (control is HotkeyListItem)
                {
                    control.Width = Math.Max(400, availableWidth);
                }
            }
            _hotkeyFlowPanel.ResumeLayout(true);
        };

        panel.Controls.Add(_hotkeyFlowPanel);
        panel.Controls.Add(_addHotkeyButton);

        return panel;
    }

    /// <summary>
    /// Handles the add hotkey button click event. Shows hotkey editor dialog.
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
    private void AddHotkeyButton_Click(object? sender, EventArgs e)
    {
        var hotkeyDialog = new HotkeyEditorDialog();
        if (hotkeyDialog.ShowDialog() == DialogResult.OK)
        {
            var hotkey = hotkeyDialog.Hotkey;

            var existingHotkey = GetHotkeysFromUI().FirstOrDefault(h => h.Action == hotkey.Action);
            if (existingHotkey != null)
            {
                MessageBoxHelper.ShowWarning(
                    $"A hotkey for '{hotkey.Action}' already exists. Please edit or delete the existing hotkey first.",
                    "Duplicate Action"
                );
                return;
            }

            AddHotkeyEntryToUI(hotkey);
            HotkeyAdded?.Invoke(this, EventArgs.Empty);
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Loads a list of hotkeys into the UI panel.
    /// </summary>
    /// <param name="hotkeys">The list of hotkeys to display</param>
    public void LoadHotkeys(List<Hotkey> hotkeys)
    {
        if (_hotkeyFlowPanel == null)
        {
            LogWarning("_hotkeyFlowPanel is null in LoadHotkeys");
            return;
        }

        _hotkeyFlowPanel.SuspendLayout();
        _hotkeyFlowPanel.Controls.Clear();
        _hotkeyEntries.Clear();

        LogInfo($"Loading {hotkeys.Count} hotkeys");

        // Deep copy for original state
        _originalHotkeys = hotkeys.Select(h => new Hotkey
        {
            Id = h.Id,
            Action = h.Action,
            KeyCode = h.KeyCode,
            Modifiers = h.Modifiers,
            IsEnabled = h.IsEnabled
        }).ToList();

        foreach (var hotkey in hotkeys)
        {
            AddHotkeyEntryToUI(hotkey);
        }

        _hotkeyFlowPanel.ResumeLayout(true);
        LogInfo($"Total controls in _hotkeyFlowPanel: {_hotkeyFlowPanel.Controls.Count}");
    }

    /// <summary>
    /// Adds a hotkey entry to the UI display.
    /// </summary>
    /// <param name="hotkey">The hotkey to add</param>
    public void AddHotkeyEntryToUI(Hotkey hotkey)
    {
        var entry = new HotkeyListItem(hotkey);
        entry.EditRequested += (s, e) => HotkeyEditRequested?.Invoke(this, hotkey);
        entry.DeleteRequested += (s, e) => HotkeyDeleteRequested?.Invoke(this, hotkey);

        _hotkeyEntries[hotkey.Id.ToString()] = entry;
        entry.Visible = true;

        _hotkeyFlowPanel.Controls.Add(entry);

        LogInfo($"Added hotkey to UI: {hotkey.Action} with ID {hotkey.Id}");
    }

    /// <summary>
    /// Updates an existing hotkey entry in the UI.
    /// </summary>
    /// <param name="hotkey">The hotkey with updated values</param>
    public void UpdateHotkeyEntry(Hotkey hotkey)
    {
        if (_hotkeyEntries.TryGetValue(hotkey.Id.ToString(), out var entry))
        {
            entry.UpdateHotkey(hotkey);
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Removes a hotkey entry from the UI.
    /// </summary>
    /// <param name="hotkeyId">The ID of the hotkey to remove</param>
    public void RemoveHotkeyEntry(int hotkeyId)
    {
        if (_hotkeyEntries.TryGetValue(hotkeyId.ToString(), out var entry))
        {
            _hotkeyFlowPanel.Controls.Remove(entry);
            _hotkeyEntries.Remove(hotkeyId.ToString());
            entry.Dispose();
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Retrieves all hotkeys currently displayed in the UI.
    /// </summary>
    /// <returns>A list of all hotkeys</returns>
    public List<Hotkey> GetHotkeysFromUI()
    {
        return _hotkeyEntries.Values.Select(entry => entry.Hotkey).ToList();
    }

    /// <summary>
    /// Checks if there are any unsaved changes in the panel.
    /// </summary>
    /// <returns>True if there are unsaved changes, false otherwise</returns>
    public bool HasUnsavedChanges()
    {
        var currentHotkeys = GetHotkeysFromUI();
        if (currentHotkeys.Count != _originalHotkeys.Count)
        {
            return true;
        }

        foreach (var original in _originalHotkeys)
        {
            var current = currentHotkeys.FirstOrDefault(h => h.Id == original.Id);
            if (current == null)
            {
                return true; // Deleted
            }

            if (current.Action != original.Action ||
                current.KeyCode != original.KeyCode ||
                current.Modifiers != original.Modifiers ||
                current.IsEnabled != original.IsEnabled)
            {
                return true; // Modified
            }
        }

        // Check for new hotkeys
        if (currentHotkeys.Any(c => !_originalHotkeys.Any(o => o.Id == c.Id)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Updates the last triggered hotkey display label with thread safety.
    /// </summary>
    /// <param name="text">The text to display</param>
    public void UpdateLastHotkeyLabel(string text)
    {
        if (_lastHotkeyLabel.InvokeRequired)
        {
            _lastHotkeyLabel.Invoke(() => _lastHotkeyLabel.Text = text);
        }
        else
        {
            _lastHotkeyLabel.Text = text;
        }
    }
}
