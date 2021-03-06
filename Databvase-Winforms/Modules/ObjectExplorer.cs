﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Databvase_Winforms.Annotations;
using Databvase_Winforms.Globals;
using Databvase_Winforms.Models;
using Databvase_Winforms.Models.Data_Providers;
using Databvase_Winforms.Services.Window_Dialog_Services;
using Databvase_Winforms.Views;
using Databvase_Winforms.View_Models;
using DevExpress.Utils.MVVM;
using DevExpress.Utils.MVVM.Services;
using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using DevExpress.XtraTreeList.Nodes;
using DevExpress.XtraTreeList.Nodes.Operations;
using Microsoft.SqlServer.Management.Smo;

namespace Databvase_Winforms.Modules
{
    public partial class ObjectExplorer : XtraUserControl
    {
        private Dictionary<string, Action> _popupActionDictionary;
        private IOverlaySplashScreenHandle _overlayHandle;

        public ObjectExplorer()
        {
            InitializeComponent();
            if (!mvvmContextObjectExplorer.IsDesignMode)
                InitializeBindings();
            MVVMContext.RegisterXtraDialogService();
            HookupEvents();
            RegisterServices();
            CreatePopUpActions();
        }

        private void HookupEvents()
        {
            treeListObjExp.PopupMenuShowing += TreeListObjectExplorerOnPopupMenuShowing;
            treeListObjExp.MouseDown += TreeListObjExpOnMouseDown;
            treeListObjExp.NodeChanged += TreeListObjExpOnNodeChanged;
            treeListObjExp.CustomRowFilter += TreeListObjExpOnCustomRowFilter;
        }


        private void RegisterServices()
        {
            mvvmContextObjectExplorer.RegisterService(new BackupViewService());
        }

        private void CreatePopUpActions()
        {
            _popupActionDictionary = new Dictionary<string, Action>
            {
                {GlobalStrings.ObjectExplorerTypes.Instance, () => popupMenuObjectExplorer.ShowPopup(MousePosition)},
                {GlobalStrings.ObjectExplorerTypes.Database, () => popupMenuDatabase.ShowPopup(MousePosition)},
                {GlobalStrings.ObjectExplorerTypes.Table, () => popupMenuTable.ShowPopup(MousePosition)},
                {GlobalStrings.ObjectExplorerTypes.View, () => popupMenuView.ShowPopup(MousePosition)},
                {GlobalStrings.ObjectExplorerTypes.Column, () => popupMenuObjectExplorer.ShowPopup(MousePosition)},
                {GlobalStrings.ObjectExplorerTypes.Folder, () => popupMenuObjectExplorer.ShowPopup(MousePosition)},
                {GlobalStrings.ObjectExplorerTypes.Function, () => popupMenuFunction.ShowPopup(MousePosition)},
                {GlobalStrings.ObjectExplorerTypes.StoredProcedure, () => popupMenuStoredProcedure.ShowPopup(MousePosition)},
                {GlobalStrings.ObjectExplorerTypes.Nothing, () => {  }} //Do nothing for nothing... lulz
            };

        }

        #region TreeList Methods

        private void TreeListObjExpOnMouseDown(object sender, MouseEventArgs e)
        {
            var treeList = sender as TreeList;
            var info = treeList?.CalcHitInfo(e.Location);
            if (info?.Node != null) treeListObjExp.FocusedNode = info.Node;
        }

        private void TreeListObjectExplorerOnPopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            var focusedNodeType = treeListObjExp.FocusedNode?.GetValue(treeListColumnType).ToString();
            if (focusedNodeType == null) return;
            var popUpMenuAction = _popupActionDictionary[focusedNodeType];
            popUpMenuAction.Invoke();
        }

        private void TreeListObjExpOnNodeChanged(object sender, NodeChangedEventArgs e)
        {   //TODO - This event is firing even when a node is already added, which may mean that this may not be the best way to flag a node as having children...
            if (e.ChangeType == NodeChangeTypeEnum.Add)
            {
                if (!(e.Node.TreeList.GetRow(e.Node.Id) is ObjectExplorerNode model) || e.Node.HasChildren) return;
                SetHasChildrenForNode(e, model);
            }
        }

        private static void SetHasChildrenForNode(NodeChangedEventArgs e, ObjectExplorerNode objectExplorerModel)
        {
            switch (objectExplorerModel.Type)
            {
                case GlobalStrings.ObjectExplorerTypes.Instance:
                    e.Node.HasChildren = true;
                    break;
                case GlobalStrings.ObjectExplorerTypes.Database:
                    e.Node.HasChildren = true;
                    break;
                case GlobalStrings.ObjectExplorerTypes.Folder:
                    e.Node.HasChildren = true;
                    break;
                case GlobalStrings.ObjectExplorerTypes.Table:
                    e.Node.HasChildren = true;
                    break;
                default:
                    e.Node.HasChildren = false;
                    break;
            }
        }

        /// <summary>
        /// Used to apply custom filter logic. For Databvase's object explorer, this is used to show the child nodes of anything that
        /// matches the filter criteria.
        /// Source: https://www.devexpress.com/Support/Center/Question/Details/T528434/treelist-how-to-show-child-nodes-if-their-parent-node-satisfies-the-currently-applied
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeListObjExpOnCustomRowFilter(object sender, CustomRowFilterEventArgs e)
        {
            if (e.Node.ParentNode != null && e.Node.ParentNode.Visible)
            {
                e.Node.Visible = true;
                e.Handled = true;
            }
        }

        #endregion


        private void ShowWaitOverlay()
        {
            if (_overlayHandle == null)
            {
                _overlayHandle = SplashScreenManager.ShowOverlayForm(this);
            }
        }

        private void CloseWaitOverlay()
        {
            if (_overlayHandle != null)
            {
                SplashScreenManager.CloseOverlayForm(_overlayHandle);
                _overlayHandle = null;
            }
        }

        private void InitializeBindings()
        {
            var fluent = mvvmContextObjectExplorer.OfType<ObjectExplorerViewModel>();
            SetBindings(fluent);
            BindCommands(fluent);
            BindEvents(fluent);

        }

        private void SetBindings(MVVMContextFluentAPI<ObjectExplorerViewModel> fluent)
        {
            fluent.SetBinding(barButtonItemGenerateSelectTopStatement, x => x.Caption,
                y => y.SelectTopContextMenuItemDescription);
            fluent.SetBinding(treeListObjExp, x => x.DataSource, vm => vm.ObjectExplorerSource);
        }

        private void BindEvents(MVVMContextFluentAPI<ObjectExplorerViewModel> fluent)
        {
            fluent.EventToCommand<BeforeExpandEventArgs>(treeListObjExp, "BeforeExpand",
                x => x.ObjectExplorer_OnBeforeExpand(null));
            fluent.WithEvent<TreeList, FocusedNodeChangedEventArgs>(treeListObjExp, "FocusedNodeChanged").SetBinding(
                x => x.FocusedNode,
                args => args.Node?.TreeList.GetDataRecordByNode(args.Node) as ObjectExplorerNode, (treeView, entity) =>
                    treeView.FocusedNode = treeView.FindNode(
                        x => x.Id == entity.Id));
        }

        private void BindCommands(MVVMContextFluentAPI<ObjectExplorerViewModel> fluent)
        {
            fluent.BindCommand(barButtonItemGenerateSelectAll, x => x.ScriptSelectAllForObjectExplorerData());
            fluent.BindCommand(barButtonItemGenerateSelectTopStatement, x => x.ScriptSelectTopForObjectExplorerData());
            fluent.BindCommand(barButtonItemAlterScript, x => x.ScriptModifyForObjectExplorerData());
            fluent.BindCommand(barButtonItemViewFunction, x => x.ScriptAlterForObjectExplorerData());
            fluent.BindCommand(barButtonItemNewQuery, x => x.NewQueryScript());
            fluent.BindCommand(barButtonItemCopyFullName, x=>x.CopyFullName());
            fluent.BindCommand(barButtonItemCreateDatabaseBackup, x => x.ShowBackupView());
            fluent.BindCommand(barButtonItemRefresh, x=>x.RefreshNode());
            fluent.SetTrigger(vm => vm.LoadingMode, TriggerAction);
        }

        private void TriggerAction(ObjectExplorerViewModel.UnboundLoadModes unboundLoadMode)
        {
            switch (unboundLoadMode)
            {
                case ObjectExplorerViewModel.UnboundLoadModes.BeginUnboundLoad:
                    treeListObjExp.BeginUnboundLoad();
                    ShowWaitOverlay();
                    break;
                case ObjectExplorerViewModel.UnboundLoadModes.FinishUnboundLoad:
                    treeListObjExp.EndUnboundLoad();
                    treeListObjExp.FocusedNode?.Expand(); //TODO - This is a hack to get the focused node to expand... see if there's a way to avoid doing this.
                    CloseWaitOverlay();
                    break;
                case ObjectExplorerViewModel.UnboundLoadModes.FinishUnboundLoadRefresh:
                    treeListObjExp.EndUnboundLoad();
                    treeListObjExp.FocusedNode?.Collapse();
                    CloseWaitOverlay();
                    break;
            }
        }

    }
}