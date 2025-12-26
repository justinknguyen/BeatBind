using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Presentation.Themes;
using BeatBind.Presentation.Helpers;
using BeatBind.Presentation.Components;
using MaterialSkin.Controls;
using Microsoft.Extensions.Logging;

namespace BeatBind.Presentation.Panels;

public partial class HotkeysPanel : BasePanelControl
{
    private readonly HotkeyApplicationService? _hotkeyApplicationService;
    
    private MaterialLabel _lastHotkeyLabel = null!;
    private FlowLayoutPanel _hotkeyFlowPanel = null!;
    private MaterialButton _addHotkeyButton = null!;
    private Dictionary<string, HotkeyListItem> _hotkeyEntries = new();
    
    public event EventHandler<Hotkey>? HotkeyEditRequested;
    public event EventHandler<Hotkey>? HotkeyDeleteRequested;
    public event EventHandler? HotkeyAdded;

    public HotkeysPanel(HotkeyApplicationService? hotkeyApplicationService, ILogger<HotkeysPanel> logger)
        : base(logger)
    {
        _hotkeyApplicationService = hotkeyApplicationService;
    }

    // Parameterless constructor for WinForms designer support
    public HotkeysPanel() : base()
    {
        _hotkeyApplicationService = null;
    }

    protected override void InitializeComponent()
    {
        SuspendLayout();
        
        Dock = DockStyle.Fill;
        BackColor = Theme.CardBackground;
        Padding = new Padding(20);
        
        ResumeLayout(false);
    }

    protected override void InitializeUI()
    {
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.CardBackground
        };

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            BackColor = Theme.CardBackground
        };

        // Use CardFactory for consistent card creation
        var lastHotkeyCard = CardFactory.CreateModernCard("Last Triggered Hotkey", CreateLastHotkeyContent());
        mainLayout.Controls.Add(lastHotkeyCard);

        var hotkeyCard = CardFactory.CreateModernCard("Hotkey Management", CreateHotkeyManagementContent(), fixedHeight: 460);
        mainLayout.Controls.Add(hotkeyCard);

        scrollPanel.Controls.Add(mainLayout);
        Controls.Add(scrollPanel);
    }

    private Control CreateLastHotkeyContent()
    {
        var panel = new Panel { Height = 40, Dock = DockStyle.Top };

        // Use ControlFactory for consistent control creation
        _lastHotkeyLabel = ControlFactory.CreateMaterialLabel("No hotkey triggered yet", highEmphasis: false);

        panel.Controls.Add(_lastHotkeyLabel);
        return panel;
    }

    private Control CreateHotkeyManagementContent()
    {
        var panel = new Panel { Height = 400, Dock = DockStyle.Top };

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
            _hotkeyApplicationService?.AddHotkey(hotkey);
            HotkeyAdded?.Invoke(this, EventArgs.Empty);
        }
    }

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

        foreach (var hotkey in hotkeys)
        {
            AddHotkeyEntryToUI(hotkey);
        }

        _hotkeyFlowPanel.ResumeLayout(true);
        LogInfo($"Total controls in _hotkeyFlowPanel: {_hotkeyFlowPanel.Controls.Count}");
    }

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

    public void UpdateHotkeyEntry(Hotkey hotkey)
    {
        if (_hotkeyEntries.TryGetValue(hotkey.Id.ToString(), out var entry))
        {
            entry.UpdateHotkey(hotkey);
        }
    }

    public void RemoveHotkeyEntry(int hotkeyId)
    {
        if (_hotkeyEntries.TryGetValue(hotkeyId.ToString(), out var entry))
        {
            _hotkeyFlowPanel.Controls.Remove(entry);
            _hotkeyEntries.Remove(hotkeyId.ToString());
            entry.Dispose();
        }
    }

    public List<Hotkey> GetHotkeysFromUI()
    {
        return _hotkeyEntries.Values.Select(entry => entry.Hotkey).ToList();
    }

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
