﻿using MahApps.Metro.Controls.Dialogs;
using NETworkManager.Models.Profile;
using NETworkManager.Models.Settings;
using NETworkManager.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NETworkManager.ViewModels
{
    public class SettingsSettingsViewModel : ViewModelBase
    {
        #region Variables
        private readonly IDialogCoordinator _dialogCoordinator;

        private readonly bool _isLoading;

        public Action CloseAction { get; set; }

        private string _locationSelectedPath;
        public string LocationSelectedPath
        {
            get => _locationSelectedPath;
            set
            {
                if (value == _locationSelectedPath)
                    return;

                _locationSelectedPath = value;
                OnPropertyChanged();
            }
        }

        private bool _movingFiles;
        public bool MovingFiles
        {
            get => _movingFiles;
            set
            {
                if (value == _movingFiles)
                    return;

                _movingFiles = value;
                OnPropertyChanged();
            }
        }

        private bool _settingsExists;
        public bool SettingsExists
        {
            get => _settingsExists;
            set
            {
                if (value == _settingsExists)
                    return;

                _settingsExists = value;
                OnPropertyChanged();
            }
        }

        private bool _resetSettings;
        public bool ResetSettings
        {
            get => _resetSettings;
            set
            {
                if (value == _resetSettings)
                    return;

                _resetSettings = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Constructor, LoadSettings
        public SettingsSettingsViewModel(IDialogCoordinator instance)
        {
            _isLoading = true;

            _dialogCoordinator = instance;

            LoadSettings();

            _isLoading = false;
        }

        private void LoadSettings()
        {
            LocationSelectedPath = SettingsManager.GetSettingsLocationNotPortable();
            //IsPortable = SettingsManager.GetIsPortable();
        }
        #endregion

        #region ICommands & Actions
        public ICommand BrowseFolderCommand => new RelayCommand(p => BrowseFolderAction());

        private void BrowseFolderAction()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (Directory.Exists(LocationSelectedPath))
                dialog.SelectedPath = LocationSelectedPath;

            var dialogResult = dialog.ShowDialog();

            if (dialogResult == System.Windows.Forms.DialogResult.OK)
                LocationSelectedPath = dialog.SelectedPath;
        }

        public ICommand OpenLocationCommand => new RelayCommand(p => OpenLocationAction());

        private static void OpenLocationAction()
        {
            Process.Start("explorer.exe", SettingsManager.GetSettingsLocation());
        }

        public ICommand ChangeSettingsCommand => new RelayCommand(p => ChangeSettingsAction());

        // Check if a file(name) is a settings file
        private static bool FilesContainsSettingsFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);

                if (SettingsManager.GetSettingsFileName() == fileName)
                    return true;

                if (ProfileManager.ProfilesFileName == fileName)
                    return true;
            }

            return false;
        }

        private async void ChangeSettingsAction()
        {
            MovingFiles = true;
            var overwrite = false;
            var forceRestart = false;

            var filesTargedLocation = Directory.GetFiles(LocationSelectedPath);

            // Check if there are any settings files in the folder...
            if (FilesContainsSettingsFiles(filesTargedLocation))
            {
                var settings = AppearanceManager.MetroDialog;

                settings.AffirmativeButtonText = Resources.Localization.Strings.Overwrite;
                settings.NegativeButtonText = Resources.Localization.Strings.Cancel;
                settings.FirstAuxiliaryButtonText = Resources.Localization.Strings.MoveAndRestart;
                settings.DefaultButtonFocus = MessageDialogResult.FirstAuxiliary;

                var result = await _dialogCoordinator.ShowMessageAsync(this, Resources.Localization.Strings.Overwrite, Resources.Localization.Strings.OverwriteSettingsInTheDestinationFolder, MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, AppearanceManager.MetroDialog);

                switch (result)
                {
                    case MessageDialogResult.Negative:
                        MovingFiles = false;
                        return;
                    case MessageDialogResult.Affirmative:
                        overwrite = true;
                        break;
                    case MessageDialogResult.FirstAuxiliary:
                        forceRestart = true;
                        break;
                }
            }

            // Try moving files (permissions, file is in use...)
            try
            {
                await SettingsManager.MoveSettingsAsync(SettingsManager.GetSettingsLocation(), LocationSelectedPath, overwrite, filesTargedLocation);

                Properties.Settings.Default.Settings_CustomSettingsLocation = LocationSelectedPath;

                // Show the user some awesome animation to indicate we are working on it :)
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                var settings = AppearanceManager.MetroDialog;

                settings.AffirmativeButtonText = Resources.Localization.Strings.OK;

                await _dialogCoordinator.ShowMessageAsync(this, Resources.Localization.Strings.Error, ex.Message, MessageDialogStyle.Affirmative, settings);
            }

            LocationSelectedPath = string.Empty;
            LocationSelectedPath = Properties.Settings.Default.Settings_CustomSettingsLocation;

            if (forceRestart)
            {
                SettingsManager.ForceRestart = true;
                CloseAction();
            }

            MovingFiles = false;
        }

        public ICommand RestoreDefaultSettingsLocationCommand => new RelayCommand(p => RestoreDefaultSettingsLocationAction());

        private void RestoreDefaultSettingsLocationAction()
        {
            LocationSelectedPath = SettingsManager.GetDefaultSettingsLocation();
        }

        public ICommand ResetSettingsCommand => new RelayCommand(p => ResetSettingsAction());

        public async void ResetSettingsAction()
        {
            var settings = AppearanceManager.MetroDialog;

            settings.AffirmativeButtonText = Resources.Localization.Strings.Continue;
            settings.NegativeButtonText = Resources.Localization.Strings.Cancel;

            settings.DefaultButtonFocus = MessageDialogResult.Affirmative;

            var message = Resources.Localization.Strings.SelectedSettingsAreReset;

            message += Environment.NewLine + Environment.NewLine + $"* {Resources.Localization.Strings.TheSettingsLocationIsNotAffected}";
            message += Environment.NewLine + $"* {Resources.Localization.Strings.ApplicationIsRestartedAfterwards}";

            if (await _dialogCoordinator.ShowMessageAsync(this, Resources.Localization.Strings.AreYouSure, message, MessageDialogStyle.AffirmativeAndNegative, settings) != MessageDialogResult.Affirmative)
                return;

            SettingsManager.Reset();

            message = Resources.Localization.Strings.SettingsSuccessfullyReset;
            message += Environment.NewLine + Environment.NewLine + Resources.Localization.Strings.TheApplicationWillBeRestarted;

            await _dialogCoordinator.ShowMessageAsync(this, Resources.Localization.Strings.Success, message, MessageDialogStyle.Affirmative, settings);

            // Restart
            CloseAction();
        }
        #endregion

        #region Methods
        public void SaveAndCheckSettings()
        {
            // Save everything
            if (SettingsManager.Current.SettingsChanged)
                SettingsManager.Save();

            // Check if files exist
            SettingsExists = File.Exists(SettingsManager.GetSettingsFilePath());
        }

        public void SetLocationPathFromDragDrop(string path)
        {
            LocationSelectedPath = path;

            OnPropertyChanged(nameof(LocationSelectedPath));
        }
        #endregion
    }
}