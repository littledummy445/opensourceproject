using System.Text;

namespace WindowsNotesApp;

public class MainForm : Form
{
    private readonly TextBox _editor;
    private readonly ToolStripStatusLabel _statusLabel;
    private string? _currentPath;
    private bool _isDirty;

    public MainForm()
    {
        Text = "Windows Notes App";
        Width = 1000;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        var menuStrip = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add("&New", null, (_, _) => CreateNewFile());
        fileMenu.DropDownItems.Add("&Open...", null, (_, _) => OpenFile());
        fileMenu.DropDownItems.Add("&Save", null, (_, _) => SaveFile());
        fileMenu.DropDownItems.Add("Save &As...", null, (_, _) => SaveFileAs());
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("E&xit", null, (_, _) => Close());

        var editMenu = new ToolStripMenuItem("&Edit");
        editMenu.DropDownItems.Add("Cu&t", null, (_, _) => _editor.Cut());
        editMenu.DropDownItems.Add("&Copy", null, (_, _) => _editor.Copy());
        editMenu.DropDownItems.Add("&Paste", null, (_, _) => _editor.Paste());
        editMenu.DropDownItems.Add("Select &All", null, (_, _) => _editor.SelectAll());

        menuStrip.Items.Add(fileMenu);
        menuStrip.Items.Add(editMenu);

        _editor = new TextBox
        {
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font("Consolas", 11),
        };
        _editor.TextChanged += (_, _) =>
        {
            _isDirty = true;
            UpdateTitle();
            UpdateStatus();
        };

        var statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Ready");
        statusStrip.Items.Add(_statusLabel);

        MainMenuStrip = menuStrip;
        Controls.Add(_editor);
        Controls.Add(statusStrip);
        Controls.Add(menuStrip);

        FormClosing += OnClosing;
        UpdateTitle();
        UpdateStatus();
    }

    private void CreateNewFile()
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        _editor.Clear();
        _currentPath = null;
        _isDirty = false;
        UpdateTitle();
        UpdateStatus();
    }

    private void OpenFile()
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        using var openDialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Open text file"
        };

        if (openDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _editor.Text = File.ReadAllText(openDialog.FileName, Encoding.UTF8);
        _currentPath = openDialog.FileName;
        _isDirty = false;
        UpdateTitle();
        UpdateStatus();
    }

    private void SaveFile()
    {
        if (string.IsNullOrWhiteSpace(_currentPath))
        {
            SaveFileAs();
            return;
        }

        File.WriteAllText(_currentPath, _editor.Text, Encoding.UTF8);
        _isDirty = false;
        UpdateTitle();
    }

    private void SaveFileAs()
    {
        using var saveDialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Save text file"
        };

        if (saveDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _currentPath = saveDialog.FileName;
        SaveFile();
    }

    private bool ConfirmDiscardChanges()
    {
        if (!_isDirty)
        {
            return true;
        }

        var result = MessageBox.Show(
            this,
            "You have unsaved changes. Do you want to save them first?",
            "Unsaved changes",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Cancel)
        {
            return false;
        }

        if (result == DialogResult.Yes)
        {
            SaveFile();
            return !_isDirty;
        }

        return true;
    }

    private void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (ConfirmDiscardChanges())
        {
            return;
        }

        e.Cancel = true;
    }

    private void UpdateTitle()
    {
        var fileName = string.IsNullOrWhiteSpace(_currentPath)
            ? "Untitled.txt"
            : Path.GetFileName(_currentPath);

        var dirtyMarker = _isDirty ? "*" : string.Empty;
        Text = $"{dirtyMarker}{fileName} - Windows Notes App";
    }

    private void UpdateStatus()
    {
        var lineCount = _editor.Lines.Length;
        var wordCount = _editor.Text
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Length;

        _statusLabel.Text = $"Lines: {lineCount}    Words: {wordCount}";
    }
}
