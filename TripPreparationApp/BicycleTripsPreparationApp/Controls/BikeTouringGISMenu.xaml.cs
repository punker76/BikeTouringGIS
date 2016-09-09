﻿using BikeTouringGIS.ViewModels;
using BikeTouringGISLibrary.Model;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BikeTouringGIS.Controls
{
    /// <summary>
    /// Interaction logic for Menu.xaml
    /// </summary>
    public partial class BikeTouringGISMenu : UserControl
    {
        public static readonly DependencyProperty ProjectProperty = DependencyProperty.Register("Project", typeof(BikeTouringGISProject), typeof(BikeTouringGISMenu), new PropertyMetadata(default(BikeTouringGISProject)));
        private readonly IDialogCoordinator _dialogCoordinator;

        public BikeTouringGISMenu()
        {
            InitializeComponent();
            _dialogCoordinator = DialogCoordinator.Instance;
        }

        public BikeTouringGISProject Project
        {
            get { return (BikeTouringGISProject)GetValue(ProjectProperty); }
            set { SetValue(ProjectProperty, value); }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveProjectFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveProjectFileDialog.Filter = "Bike touring GIS project files (*.biketouringgis)|*.biketouringgis";
            saveProjectFileDialog.InitialDirectory = DropBoxHelper.GetDropBoxFolder();
            if (saveProjectFileDialog.ShowDialog() == true)
            {
                var newProjectDialog = new CustomDialog() { Title = "Create new Bike touring GIS project" };
                var mainWindow = FindMainScreen() as MainScreen;

                var newProjectDialogContent = new NewProjectDialogContent(instance =>
                {
                    _dialogCoordinator.HideMetroDialogAsync(mainWindow.DataContext, newProjectDialog);
                    var x = (newProjectDialog.DataContext as NewProjectDialogContent).Description;
                });
                newProjectDialogContent.Description = "test";
                newProjectDialog.Content = new NewProjectDialog { DataContext = newProjectDialogContent};
                await _dialogCoordinator.ShowMetroDialogAsync(mainWindow.DataContext, newProjectDialog);
            }

        }

        private DependencyObject FindMainScreen()
        {
            DependencyObject ucParent = Parent;
            while (!(ucParent is MainScreen))
            {
                ucParent = LogicalTreeHelper.GetParent(ucParent);
            }
            return ucParent;
        }
    }
}