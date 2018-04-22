﻿using System;
using System.Windows.Forms;
using Databvase_Winforms.Globals;
using Databvase_Winforms.Models;
using Databvase_Winforms.View_Models;
using DevExpress.XtraEditors;
using DevExpress.XtraTreeList;
using Microsoft.SqlServer.Management.Smo;

namespace Databvase_Winforms.Modules
{
    public partial class ObjectExplorer : XtraUserControl
    {
        public ObjectExplorer()
        {
            InitializeComponent();
            if (!mvvmContextObjectExplorer.IsDesignMode)
                InitializeBindings();

            HookupEvents();
        }

        private void HookupEvents()
        {
            treeListObjExp.PopupMenuShowing += TreeListObjectExplorerOnPopupMenuShowing;
            treeListObjExp.MouseDown += TreeListObjExpOnMouseDown;
            barButtonItemCopy.ItemClick += CopyCell;
            treeListObjExp.NodeChanged += TreeListObjExpOnNodeChanged;
        }

        private void TreeListObjExpOnNodeChanged(object sender, NodeChangedEventArgs e)
        {
            if (e.ChangeType != NodeChangeTypeEnum.Add) return;
            if (!(e.Node.TreeList.GetRow(e.Node.Id) is ObjectExplorerModel model)) return;
            SetHasChildrenForNode(e, model);
        }

        private static void SetHasChildrenForNode(NodeChangedEventArgs e, ObjectExplorerModel objectExplorerModel)
        {
            switch (objectExplorerModel.Type)
            {
                case GlobalStrings.ObjectExplorerTypes.Instance:
                    e.Node.HasChildren = true;
                    break;
                case GlobalStrings.ObjectExplorerTypes.Database:
                    e.Node.HasChildren = true;
                    break;
                case GlobalStrings.ObjectExplorerTypes.Table:
                    e.Node.HasChildren = true;
                    break;
                case GlobalStrings.ObjectExplorerTypes.Column:
                    e.Node.HasChildren = false;
                    break;
            }
        }

        private void CopyCell(object sender, EventArgs e)
        {
            Clipboard.SetText(GetFocusedNodeFullName());
        }

        private void InitializeBindings()
        {
            var fluent = mvvmContextObjectExplorer.OfType<ObjectExplorerViewModel>();
            fluent.SetBinding(barButtonItemGenerateSelectTopStatement, x => x.Caption,
                y => y.SelectTopContextMenuItemDescription);
            fluent.BindCommand(barButtonItemGenerateSelectAll, (x, p) => x.ScriptSelectAllForTable(p),
                x => GetFocusedNodeTable());
            fluent.BindCommand(barButtonItemGenerateSelectTopStatement, (x, p) => x.ScriptSelectTopForTable(p),
                x => GetFocusedNodeTable());
            fluent.EventToCommand<BeforeExpandEventArgs>(treeListObjExp, "BeforeExpand",
                x => x.ObjectExplorer_OnBeforeExpand(null));
            fluent.EventToCommand<FocusedNodeChangedEventArgs>(treeListObjExp, "FocusedNodeChanged",
                x => x.FocusedNodeChanged(null));
            fluent.SetBinding(treeListObjExp, x => x.DataSource, vm => vm.ObjectExplorerSource);
        }

        #region TreeList Methods

        private void TreeListObjExpOnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var treeList = sender as TreeList;
            var info = treeList.CalcHitInfo(e.Location);
            if (info?.Node != null) treeListObjExp.FocusedNode = info.Node;
        }

        private void TreeListObjectExplorerOnPopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            var focusedNodeType = treeListObjExp.FocusedNode?.GetValue(treeListColumnType).ToString();

            switch (focusedNodeType)
            {
                case GlobalStrings.ObjectExplorerTypes.Table:
                    popupMenuTable.ShowPopup(MousePosition);
                    break;
                default:
                    popupMenuObjectExplorer.ShowPopup(MousePosition);
                    break;
            }
        }

        #endregion

        #region Focused Node Methods

        private string GetFocusedNodeFullName()
        {
            return treeListObjExp.FocusedNode?.GetValue(treeListColumnFullName).ToString();
        }

        private Table GetFocusedNodeTable()
        {
            return (Table) treeListObjExp.FocusedNode?.GetValue(treeListColumnData);
        }

        #endregion
    }
}