using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using Lumos.Models;
using Lumos.Services.Interfaces;

namespace Lumos.UI;

public sealed class ProfilesForm : Form
{
    private readonly BindingSource _bindingSource = new();
    private readonly DataGridView _grid;
    private readonly TextBox _profileSetNameTextBox;
    private readonly ComboBox _profileSetComboBox;
    private readonly List<ProfileSet> _profileSets;

    public ProfilesForm(List<AppProfile> profiles, List<ProfileSet> profileSets, IThemeService? themeService = null)
    {
        Text = "Lumos Profiles";
        Width = 640;
        Height = 420;
        StartPosition = FormStartPosition.CenterScreen;

        _profileSets = profileSets;
        _bindingSource.DataSource = new BindingList<AppProfile>(profiles.Select(CloneProfile).ToList());

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
        };

        _profileSetNameTextBox = new TextBox
        {
            Left = 12,
            Top = 12,
            Width = 180,
            PlaceholderText = "Profile set name",
        };

        _profileSetComboBox = new ComboBox
        {
            Left = 210,
            Top = 12,
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DataSource = profileSets.Select(set => set.Name).ToList(),
        };

        var loadSetButton = new Button
        {
            Left = 400,
            Top = 10,
            Width = 90,
            Text = "Load Set",
        };
        loadSetButton.Click += (_, _) => LoadSelectedProfileSet();

        var saveSetButton = new Button
        {
            Left = 500,
            Top = 10,
            Width = 100,
            Text = "Save Set",
        };
        saveSetButton.Click += (_, _) => SaveCurrentProfileSet();

        var importButton = new Button
        {
            Left = 400,
            Top = 38,
            Width = 90,
            Text = "Import",
        };
        importButton.Click += (_, _) => ImportProfiles();

        var exportButton = new Button
        {
            Left = 500,
            Top = 38,
            Width = 100,
            Text = "Export",
        };
        exportButton.Click += (_, _) => ExportProfiles();

        topPanel.Controls.Add(_profileSetNameTextBox);
        topPanel.Controls.Add(_profileSetComboBox);
        topPanel.Controls.Add(loadSetButton);
        topPanel.Controls.Add(saveSetButton);
        topPanel.Controls.Add(importButton);
        topPanel.Controls.Add(exportButton);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            BackgroundColor = Color.FromArgb(32, 32, 36),
            BorderStyle = BorderStyle.None,
            DataSource = _bindingSource,
        };

        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
        };

        var deleteButton = new Button
        {
            Left = 12,
            Top = 8,
            Width = 120,
            Text = "Delete Selected",
        };
        deleteButton.Click += (_, _) => DeleteSelectedProfile();

        var saveButton = new Button
        {
            Left = 500,
            Top = 8,
            Width = 100,
            Text = "Save",
            DialogResult = DialogResult.OK,
        };

        bottomPanel.Controls.Add(deleteButton);
        bottomPanel.Controls.Add(saveButton);

        Controls.Add(_grid);
        Controls.Add(topPanel);
        Controls.Add(bottomPanel);

        AcceptButton = saveButton;

        themeService?.ApplyTheme(this, "dark");
    }

    public List<AppProfile> GetProfiles() =>
        ((BindingList<AppProfile>)_bindingSource.DataSource!).Select(CloneProfile).ToList();

    public ProfileSet? PendingSavedProfileSet { get; private set; }

    private void DeleteSelectedProfile()
    {
        if (_grid.CurrentRow?.DataBoundItem is AppProfile profile)
        {
            ((BindingList<AppProfile>)_bindingSource.DataSource!).Remove(profile);
        }
    }

    private void LoadSelectedProfileSet()
    {
        var selectedName = _profileSetComboBox.SelectedItem as string;
        var selectedSet = _profileSets.FirstOrDefault(set => set.Name == selectedName);
        if (selectedSet is null)
        {
            return;
        }

        _bindingSource.DataSource = new BindingList<AppProfile>(selectedSet.Profiles.Select(CloneProfile).ToList());
        _grid.DataSource = _bindingSource;
    }

    private void SaveCurrentProfileSet()
    {
        var name = string.IsNullOrWhiteSpace(_profileSetNameTextBox.Text) ? "default" : _profileSetNameTextBox.Text.Trim();
        PendingSavedProfileSet = new ProfileSet
        {
            Name = name,
            Profiles = GetProfiles(),
        };

        MessageBox.Show(this, $"Profile set '{name}' is ready to save.", "Lumos", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ImportProfiles()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var json = File.ReadAllText(dialog.FileName);
        var profiles = JsonSerializer.Deserialize<List<AppProfile>>(json) ?? [];
        _bindingSource.DataSource = new BindingList<AppProfile>(profiles.Select(CloneProfile).ToList());
        _grid.DataSource = _bindingSource;
    }

    private void ExportProfiles()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = "profiles.json",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var json = JsonSerializer.Serialize(GetProfiles(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dialog.FileName, json);
    }

    private static AppProfile CloneProfile(AppProfile profile) =>
        new()
        {
            ExeName = profile.ExeName,
            DisplayName = profile.DisplayName,
            Brightness = profile.Brightness,
            Excluded = profile.Excluded,
            LastUpdatedUtc = profile.LastUpdatedUtc,
        };
}
