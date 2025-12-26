using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Presentation.Themes;
using BeatBind.Presentation.Components;
using MaterialSkin.Controls;
using Microsoft.Extensions.Logging;

namespace BeatBind.Presentation.Panels;

public partial class HotkeysPanel : UserControl
{
    private readonly HotkeyApplicationService? _hotkeyApplicationService;
    private readonly ILogger<HotkeysPanel> _logger;
    
    private MaterialLabel _lastHotkeyLabel = null!;
    private FlowLayoutPanel _hotkeyFlowPanel = null!;
    private MaterialButton _addHotkeyButton = null!;
    private Dictionary<string, HotkeyListItem> _hotkeyEntries = new();
    
    public event EventHandler<Hotkey>? HotkeyEditRequested;
    public event EventHandler<Hotkey>? HotkeyDeleteRequested;
    public event EventHandler? HotkeyAdded;

    public HotkeysPanel(HotkeyApplicationService? hotkeyApplicationService, ILogger<HotkeysPanel> logger)
    {
        _hotkeyApplicationService = hotkeyApplicationService;
        _logger = logger;
        
        InitializeComponent();
        InitializeUI();
    }

    // Parameterless constructor for WinForms designer support
    public HotkeysPanel()
    {
        _hotkeyApplicationService = null;
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<HotkeysPanel>.Instance;
        
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        
        Dock = DockStyle.Fill;
        BackColor = Theme.CardBackground;
        Padding = new Padding(20);
        
        ResumeLayout(false);
    }

    private void InitializeUI()
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

        // Last Hotkey Status Card
        var lastHotkeyCard = CreateModernCard("Last Triggered Hotkey", CreateLastHotkeyContent());
        mainLayout.Controls.Add(lastHotkeyCard);

        // Hotkey Management Card
        var hotkeyCard = CreateModernCard("Hotkey Management", CreateHotkeyManagementContent());
        mainLayout.Controls.Add(hotkeyCard);

        scrollPanel.Controls.Add(mainLayout);
        Controls.Add(scrollPanel);
    }

    private Panel CreateModernCard(string title, Control content)
    {
        var card = new Panel
        {
            Dock = DockStyle.Top,
            Height = content.Height + 60,
            Margin = new Padding(0, 0, 0, 15),
            BackColor = Theme.CardBackground,
            BorderStyle = BorderStyle.None
        };

        card.Paint += (s, e) =>
        {
            var rect = card.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            e.Graphics.DrawRectangle(new Pen(Theme.Border), rect);
        };

        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Theme.HeaderBackground,
            Tag = "headerPanel",
            Padding = new Padding(15, 0, 15, 0)
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Theme.PrimaryText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
            Tag = "headerLabel"
        };
        headerPanel.Controls.Add(titleLabel);

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15, 15, 15, 15),
            BackColor = Theme.CardBackground
        };
        contentPanel.Controls.Add(content);

        card.Controls.Add(contentPanel);
        card.Controls.Add(headerPanel);

        headerPanel.SendToBack();
        contentPanel.BringToFront();

        return card;
    }

    private Control CreateLastHotkeyContent()
    {
        var panel = new Panel { Height = 40, Dock = DockStyle.Top };

        _lastHotkeyLabel = new MaterialLabel
        {
            Text = "No hotkey triggered yet",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10f),
            HighEmphasis = false
        };

        panel.Controls.Add(_lastHotkeyLabel);
        return panel;
    }

    private Control CreateHotkeyManagementContent()
    {
        var panel = new Panel { Height = 400, Dock = DockStyle.Top };

        _addHotkeyButton = new MaterialButton
        {
            Text = "ADD NEW HOTKEY",
            Size = new Size(180, 40),
            Type = MaterialButton.MaterialButtonType.Contained,
            Depth = 0,
            Location = new Point(0, 8),
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 10),
            UseAccentColor = false,
            AutoSize = false,
            Cursor = Cursors.Hand
        };
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
                MessageBox.Show($"A hotkey for '{hotkey.Action}' already exists. Please edit or delete the existing hotkey first.",
                    "Duplicate Action", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            _logger.LogWarning("_hotkeyFlowPanel is null in LoadHotkeys");
            return;
        }

        _hotkeyFlowPanel.SuspendLayout();
        _hotkeyFlowPanel.Controls.Clear();
        _hotkeyEntries.Clear();

        _logger.LogInformation($"Loading {hotkeys.Count} hotkeys");

        foreach (var hotkey in hotkeys)
        {
            AddHotkeyEntryToUI(hotkey);
        }

        _hotkeyFlowPanel.ResumeLayout(true);
        _logger.LogInformation($"Total controls in _hotkeyFlowPanel: {_hotkeyFlowPanel.Controls.Count}");
    }

    public void AddHotkeyEntryToUI(Hotkey hotkey)
    {
        var entry = new HotkeyListItem(hotkey);
        entry.EditRequested += (s, e) => HotkeyEditRequested?.Invoke(this, hotkey);
        entry.DeleteRequested += (s, e) => HotkeyDeleteRequested?.Invoke(this, hotkey);

        _hotkeyEntries[hotkey.Id.ToString()] = entry;
        entry.Visible = true;

        _hotkeyFlowPanel.Controls.Add(entry);

        _logger.LogInformation($"Added hotkey to UI: {hotkey.Action} with ID {hotkey.Id}");
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
