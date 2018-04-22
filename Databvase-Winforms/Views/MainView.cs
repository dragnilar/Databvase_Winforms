﻿using System;
using System.Linq;
using System.Windows.Forms;
using Databvase_Winforms.Dialogs;
using Databvase_Winforms.Messages;
using Databvase_Winforms.Models;
using Databvase_Winforms.Modules;
using Databvase_Winforms.Services;
using Databvase_Winforms.View_Models;
using DevExpress.Customization;
using DevExpress.LookAndFeel;
using DevExpress.Mvvm;
using DevExpress.Utils.Menu;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Docking2010.Views;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors.ColorWheel;

namespace Databvase_Winforms.Views
{
    public partial class MainView : RibbonForm
    {
        private int _numberOfQueries;

        public MainView()
        {
            InitializeComponent();
            if (!mvvmContextMain.IsDesignMode)
                InitializeBindings();
            AddObjectExplorerToUi();
            App.Skins.LoadSkinSettings();
            HookupEvents();
            RegisterMessages();
            RegisterServices();
        }

        private void AddObjectExplorerToUi()
        {
            objectExplorerContainer.Controls.Add(new ObjectExplorer {Dock = DockStyle.Fill});
        }

        private void HookupEvents()
        {
            UserLookAndFeel.Default.StyleChanged += Default_StyleChanged;
            barButtonItemColorMixer.ItemClick += barButtonItemColorMixer_ItemClick;
            barButtonItemColorPalette.ItemClick += barButtonItemColorPalette_ItemClick;
            barButtonItemConnect.ItemClick += BarButtonItemConnectOnItemClick;
            barButtonItemObjectExplorer.ItemClick += BarButtonItemObjectExplorerOnItemClick;
            barButtonItemDisconnect.ItemClick += BarButtonItemDisconnectOnItemClick;
            barButtonItemTextEditorFontSettings.ItemClick += BarButtonItemTextEditorFontSettingsOnItemClick;
            
            tabbedViewMain.PopupMenuShowing += TabbedViewMainOnPopupMenuShowing;
            tabbedViewMain.DocumentActivated += TabbedViewMainOnDocumentActivated;

            defaultLookAndFeelMain.LookAndFeel.StyleChanged += LookAndFeelOnStyleChanged;
        }

        private void RegisterMessages()
        {
            Messenger.Default.Register<NewScriptMessage>(this, NewScriptMessage.NewScriptSender, CreateNewQueryPaneWithScript);
        }

        private void RegisterServices()
        {
            mvvmContextMain.RegisterService(new SettingsWindowService());
        }

        private void LookAndFeelOnStyleChanged(object sender, EventArgs eventArgs)
        {
            App.Skins.SaveSkinSettings();
        }


        private void BarButtonItemObjectExplorerOnItemClick(object sender, ItemClickEventArgs itemClickEventArgs)
        {
            objectExplorerContainer.Panel.Show();
        }


        private void BarButtonItemConnectOnItemClick(object sender, ItemClickEventArgs itemClickEventArgs)
        {
            Connect();
        }

        private void Connect()
        {
            var window = new ConnectionStringView {StartPosition = FormStartPosition.CenterScreen};
            window.ShowDialog();
            window.Dispose();
            UpdateUIToShowConnections();
        }

        private void UpdateUIToShowConnections()
        {
            objectExplorerContainer.Show();
            UpdateConnectionStatusOnRibbon();
            var tracker = new InstanceAndDatabaseTracker
            {
                InstanceName = App.Connection.CurrentConnections.Last().Instance,
                DatabaseObject = null
            };

            Messenger.Default.Send(new InstanceConnectedMessage(tracker), InstanceConnectedMessage.ConnectInstanceSender);
        }

        private void UpdateConnectionStatusOnRibbon()
        {
            if (App.Connection.CurrentConnections.Count > 0)
            {
                barButtonItemDisconnect.Visibility = BarItemVisibility.Always;
                barButtonItemNewQuery.Visibility = BarItemVisibility.Always;
            }
            else
            {
                barButtonItemDisconnect.Visibility = BarItemVisibility.Never;
                barButtonItemNewQuery.Visibility = BarItemVisibility.Never;
            }
        }

        private void BarButtonItemDisconnectOnItemClick(object sender, ItemClickEventArgs itemClickEventArgs)
        {
            Disconnect();
        }

        private void Disconnect()
        {
            Messenger.Default.Send(new DisconnectInstanceMessage(App.Connection.InstanceTracker.InstanceName), DisconnectInstanceMessage.DisconnectInstanceSender);
            UpdateConnectionStatusOnRibbon();
        }


        private void MergeMainRibbon(QueryControl queryControl)
        {
            if (queryControl != null) ribbonControlMain.MergeRibbon(queryControl.Ribbon);
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

        private void CreateNewQueryPaneWithScript(NewScriptMessage message)
        {
            mvvmContextMain.GetViewModel<MainViewModel>().AddNewTab();
            ((QueryControl)tabbedViewMain.ActiveDocument.Control).ProcessNewScriptMessage(message);


        }



        #endregion




        #region Skin Goodness

        private void Default_StyleChanged(object sender, EventArgs e)
        {
            barButtonItemColorPalette.Visibility = LookAndFeel.ActiveSkinName == SkinStyle.Bezier
                ? BarItemVisibility.Always
                : BarItemVisibility.Never;
        }

        private void barButtonItemColorMixer_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (var colorWheel = new ColorWheelForm())
            {
                colorWheel.StartPosition = FormStartPosition.CenterParent;
                colorWheel.BeginUpdate();
                colorWheel.SkinMaskColor = UserLookAndFeel.Default.SkinMaskColor;
                colorWheel.SkinMaskColor2 = UserLookAndFeel.Default.SkinMaskColor2;
                colorWheel.EndUpdate();
                colorWheel.ShowDialog();
            }
        }

        private void barButtonItemColorPalette_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (var svgSkinSelector = new SvgSkinPaletteSelector(this))
            {
                svgSkinSelector.ShowDialog();
            }
        }

        private void BarButtonItemTextEditorFontSettingsOnItemClick(object sender, ItemClickEventArgs e)
        {
            using (var dialog = new TextEditorFontChangeDialog{StartPosition = FormStartPosition.CenterScreen} )
            {
                dialog.ShowDialog();
                Messenger.Default.Send(new SettingsUpdatedMessage(SettingsUpdatedMessage.SettingsUpdateType.TextEditorFontStyle),
                    SettingsUpdatedMessage.SettingsUpdatedSender);
            }
        }

        #endregion


        private void InitializeBindings() 
        {
            var fluent = mvvmContextMain.OfType<MainViewModel>();
            fluent.EventToCommand<ItemClickEventArgs>(barButtonItemNewQuery, "ItemClick", x => x.AddNewTab());
            fluent.EventToCommand<ItemClickEventArgs>(barButtonItemShowSettings, "ItemClick", x => x.ShowSettings());
            fluent.SetBinding(barEditItemTextEditorBG, x => x.EditValue, vm => vm.TextEditorBackgroundColor);
            fluent.SetBinding(barEditItemTextEditorLineNumberColor, x => x.EditValue, vm => vm.TextEditorLineNumberColor);
        }
    }
}