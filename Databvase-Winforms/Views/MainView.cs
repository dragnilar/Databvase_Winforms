﻿using System;
using System.Linq;
using System.Windows.Forms;
using Databvase_Winforms.Dialogs;
using Databvase_Winforms.Messages;
using Databvase_Winforms.Models;
using Databvase_Winforms.Modules;
using Databvase_Winforms.Services;
using Databvase_Winforms.Services.Window_Dialog_Services;
using Databvase_Winforms.View_Models;
using DevExpress.Customization;
using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Native.Sql;
using DevExpress.DataAccess.Sql;
using DevExpress.DataAccess.UI.Sql;
using DevExpress.LookAndFeel;
using DevExpress.Mvvm;
using DevExpress.Utils.Menu;
using DevExpress.Utils.MVVM.Services;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Docking2010.Views;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.ColorWheel;

namespace Databvase_Winforms.Views
{
    public partial class MainView : RibbonForm
    {
        public MainView()
        {
            InitializeComponent();
            RegisterServices();
            RegisterMessages();
            if (!mvvmContextMain.IsDesignMode)
                InitializeBindings();
            AddObjectExplorerToUi();
            App.Skins.LoadSkinSettings();
            HookupEvents();

        }

        private void AddObjectExplorerToUi()
        {
            objectExplorerContainer.Controls.Add(new ObjectExplorer {Dock = DockStyle.Fill});
        }

        private void HookupEvents()
        {
            UserLookAndFeel.Default.StyleChanged += Default_StyleChanged;
            barButtonItemObjectExplorer.ItemClick += BarButtonItemObjectExplorerOnItemClick;
            barButtonItemQueryBuilder.ItemClick += BarButtonItemQueryBuilderOnItemClick;
            tabbedViewMain.PopupMenuShowing += TabbedViewMainOnPopupMenuShowing;
            tabbedViewMain.DocumentActivated += TabbedViewMainOnDocumentActivated;
            defaultLookAndFeelMain.LookAndFeel.StyleChanged += LookAndFeelOnStyleChanged;
        }

        private void RegisterMessages()
        {
            Messenger.Default.Register<NewScriptMessage>(this, typeof(NewScriptMessage).Name, CreateNewQueryPaneWithScript);
        }

        private void RegisterServices()
        {
            mvvmContextMain.RegisterService(new SettingsWindowService());
            mvvmContextMain.RegisterService(new TextEditorFontChangeService());
            mvvmContextMain.RegisterService(new ConnectionWindowService());
            mvvmContextMain.RegisterService(App.Skins);
            mvvmContextMain.RegisterService(SplashScreenService.Create(splashScreenManagerMainWait));
        }

        private void Default_StyleChanged(object sender, EventArgs e)
        {
            barButtonItemColorPalette.Visibility = LookAndFeel.ActiveSkinName == SkinStyle.Bezier
                ? BarItemVisibility.Always
                : BarItemVisibility.Never;
        }

        private void LookAndFeelOnStyleChanged(object sender, EventArgs eventArgs)
        {
            App.Skins.SaveSkinSettings();
        }


        private void BarButtonItemObjectExplorerOnItemClick(object sender, ItemClickEventArgs itemClickEventArgs)
        {
            objectExplorerContainer.Panel.Show();
        }

        #region Tabbed Main View

        private void TabbedViewMainOnPopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            var menu = e.Menu;
            if (e.HitInfo.Document != null) menu.Items.Add(new DXMenuItem("Rename Tab", RenameTab));
        }

        private void RenameTab(object sender, EventArgs e)
        {
            var renameTabDialog = new RenameTabDialog {StartPosition = FormStartPosition.CenterParent};
            var dialogResult = renameTabDialog.ShowDialog();
            if (dialogResult == DialogResult.OK) tabbedViewMain.ActiveDocument.Caption = renameTabDialog.NewTabName;
            renameTabDialog.Dispose();
        }

        private void TabbedViewMainOnDocumentActivated(object sender, DocumentEventArgs e)
        {
            
            MergeMainRibbon(e.Document.Control as QueryControl);
        }

        private void MergeMainRibbon(QueryControl queryControl)
        {
            if (queryControl != null) ribbonControlMain.MergeRibbon(queryControl.Ribbon);
        }

        private void CreateNewQueryPaneWithScript(NewScriptMessage message)
        {
            mvvmContextMain.GetViewModel<MainViewModel>().AddNewTab();
            ((QueryControl)tabbedViewMain.ActiveDocument.Control).ProcessNewScriptMessage(message);


        }



        #endregion


        private void BarButtonItemQueryBuilderOnItemClick(object sender, ItemClickEventArgs e)
        {
            //TODO - Clean up
            if (App.Connection.InstanceTracker.CurrentDatabase == null)
            {
                XtraMessageBox.Show("Please select a database from the object explorer first.");
                return;
            }

            var currentServer = App.Connection.GetServerAtSpecificInstance(App.Connection.InstanceTracker.CurrentInstance.Name, App.Connection.InstanceTracker.CurrentDatabase.Name);
            var dxConnectionStringParameters =
                new CustomStringConnectionParameters(currentServer.ConnectionContext.ConnectionString);
            var dxSqlDataSource = new SqlDataSource(dxConnectionStringParameters);
            dxSqlDataSource.AddQueryWithQueryBuilder();

            var query = (dxSqlDataSource.Queries.FirstOrDefault() as SelectQuery)?.GetSql(dxSqlDataSource.Connection
                .GetDBSchema());
            new NewScriptMessage(query, App.Connection.InstanceTracker.CurrentDatabase.Name);

        }

        private static string GetSqlQueryText(string query)
        {
            return AutoSqlWrapHelper.AutoSqlTextWrap(query, 9999);
        }


        private void InitializeBindings() 
        {
            var fluent = mvvmContextMain.OfType<MainViewModel>();
            fluent.EventToCommand<ItemClickEventArgs>(barButtonItemNewQuery, "ItemClick", x => x.AddNewTab());
            fluent.EventToCommand<ItemClickEventArgs>(barButtonItemShowSettings, "ItemClick", x => x.ShowSettings());
            fluent.EventToCommand<EventArgs>(this, "Shown", x => x.CheckToShowConnectionsAtStartup());
            fluent.SetBinding(barEditItemTextEditorBG, x => x.EditValue, vm => vm.TextEditorBackgroundColor);
            fluent.SetBinding(barEditItemTextEditorLineNumberColor, x => x.EditValue, vm => vm.TextEditorLineNumberColor);
            fluent.BindCommand(barButtonItemTextEditorFontSettings, vm => vm.ShowTextEditorFontDialog());
            fluent.BindCommand(barButtonItemColorPalette, vm => vm.ShowBezierPaletteSwitcher());
            fluent.BindCommand(barButtonItemColorMixer, vm => vm.ShowColorMixer());
            fluent.BindCommand(barButtonItemConnect, vm => vm.Connect());
            fluent.BindCommand(barButtonItemDisconnect, vm => vm.Disconnect());

            fluent.SetTrigger(x => x.InstancesConnected, connectionsActive =>
            {
                if (connectionsActive)
                {
                    barButtonItemDisconnect.Visibility = BarItemVisibility.Always;
                    barButtonItemNewQuery.Visibility = BarItemVisibility.Always;
                    barButtonItemQueryBuilder.Visibility = BarItemVisibility.Always;
                }
                else
                {
                    barButtonItemDisconnect.Visibility = BarItemVisibility.Never;
                    barButtonItemNewQuery.Visibility = BarItemVisibility.Never;
                    barButtonItemQueryBuilder.Visibility = BarItemVisibility.Never;
                }
            });
        }
    }
}