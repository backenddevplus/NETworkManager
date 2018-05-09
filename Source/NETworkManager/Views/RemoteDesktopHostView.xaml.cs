﻿using MahApps.Metro.Controls.Dialogs;
using NETworkManager.ViewModels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NETworkManager.Views
{
    public partial class RemoteDesktopHostView : UserControl
    {
        RemoteDesktopHostViewModel viewModel = new RemoteDesktopHostViewModel(DialogCoordinator.Instance);

        private bool loaded = false;

        public RemoteDesktopHostView()
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            loaded = true;
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;
            menu.DataContext = viewModel;
        }

        private void ListBoxItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                viewModel.ConnectSessionCommand.Execute(null);
        }

        public async void AddTab(string host)
        {
            // Wait for the interface to load, before displaying the dialog to connect a new session... 
            // MahApps will throw an exception... 
            while (!loaded)
                await Task.Delay(100);

            if (viewModel.IsRDP8dot1Available)
                viewModel.AddTab(host);
        }
    }
}
