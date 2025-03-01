/**************************************************************************************
    Stampfer - Gothic Script Editor
    Copyright (C)  2009  Alexander "Sumpfkrautjunkie" Ruppert

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
**************************************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using PeterInterface;
using ICSharpCode.TextEditor;
using System.IO;

namespace Peter
{
    public partial class ctrlCodeStructure : DockContent, IPeterPluginTab
    {
        private IPeterPluginHost m_Host;
        private bool m_CanScroll;
      //  private TextEditorControl m_Editor;
        private Editor m_Editor;
        

        public ctrlCodeStructure()
        {
           
            InitializeComponent();
            this.m_CanScroll = true;
            this.TabText = "Code Struktur";
            this.treeMain.AfterSelect += new TreeViewEventHandler(treeMain_AfterSelect);
        }

        #region -= Helpers =-

        /// <summary>
        /// Gets or Sets if we are able to scroll to the selected item...
        /// </summary>
        public bool CanScroll
        {
            get { return this.m_CanScroll; }

            set { this.m_CanScroll = value; }
        }

        /// <summary>
        /// After an item is selected, scroll to it...
        /// </summary>
        /// <param name="sender">TreeView</param>
        /// <param name="e">TreeViewEventArgs</param>
        void treeMain_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (this.tsbGotoCode.Checked)
            {
                if (this.m_CanScroll)
                {
                    if (e.Node.Parent != null)
                    {
                        if (e.Node.Tag != null)
                        {

                            int off = Convert.ToInt32(e.Node.Tag.ToString());
                        
                            int index = e.Node.Text.IndexOf(":");
                            int temp = e.Node.Text.IndexOf("(");
                            if ((temp < index && temp > 0) || index == -1)
                            {
                                index = temp;
                            }
                            temp = e.Node.Text.IndexOf(" ");
                            if ((temp < index && temp > 0) || index == -1)
                            {
                                index = temp;
                            }
                            int pos;
                            if (index < 0)
                            {
                                pos = off + e.Node.Text.Length;
                            }
                            else
                            {
                                pos = off + index;
                            }
                        
                            this.ScrollToOffset(off, pos);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Occurs when the active content is changed...
        /// </summary>
        /// <param name="content">New Content</param>
       // public void ActiveContentChanged (IDockContent content)
        public void ActiveContentChanged(Editor e)
        {
            //
            

          //  DockContent d = (DockContent)content;
          //  TextEditorControl edit = null;
           /* foreach (Control c in d.Controls)
            {
              

                if (c.GetType().ToString().Equals(typeof(TextEditorControl).ToString()))
                {
                   
                    edit = (TextEditorControl)c;
                    break;
                }
            }*/
            //if (edit != this.m_Editor)
            if (e!=this.m_Editor)
            {
               
               // this.m_Editor = edit;
                this.m_Editor = e;
                this.ParseCode();
            }
            if (this.ItemCount == 0 && this.m_Editor != null)
            {
               
                this.ParseCode();
            }
         
           
        }

        /// <summary>
        /// Scrolls the active editor to the given offset...
        /// </summary>
        /// <param name="offset">offset to start at</param>
        /// <param name="pos">offset to end at</param>
        public void ScrollToOffset (int offset, int pos)
        {

            m_Editor.JumpToPos(offset, pos);
           /*int line = this.m_Editor.Document.GetLineNumberForOffset(offset);
            this.m_Editor.ActiveTextAreaControl.Caret.Position = this.m_Editor.Document.OffsetToPosition(offset);
            this.m_Editor.ActiveTextAreaControl.SelectionManager.SetSelection(
                this.m_Editor.Document.OffsetToPosition(offset), this.m_Editor.Document.OffsetToPosition(pos));
            this.m_Editor.ActiveTextAreaControl.CenterViewOn(line, 0);
            this.m_Editor.ActiveTextAreaControl.TextArea.Focus();*/
           
            
        }

        /// <summary>
        /// Clears the list view's items and groups...
        /// </summary>
        public void Clear()
        {
            try
            {
                this.treeMain.Nodes.Clear();
            }
            catch { }
        }

        /// <summary>
        /// Adds a node to the tree...
        /// </summary>
        /// <param name="node">Node to Add</param>
        public void AddNode(TreeNode node)
        {
            this.treeMain.Nodes.Add(node);
        }

        /// <summary>
        /// Gets the number of Items in the TreeView...
        /// </summary>
        public int ItemCount
        {
            get { return this.treeMain.Nodes.Count; }
        }

        /// <summary>
        /// Gets the Nodes for the tree...
        /// </summary>
        /// <returns>TreeNodeCollection</returns>
        public TreeNodeCollection TreeNodes()
        {
            return this.treeMain.Nodes;
        }

        /// <summary>
        /// Sets the image list of the tree...
        /// </summary>
        /// <param name="fileExt">Image list name to set...</param>
        public void SetImageList(string fileExt)
        {
            switch (fileExt)
            {
                case ".css":
                    this.treeMain.ImageList = imgCSS;
                    break;
                default:
                    this.treeMain.ImageList = null;
                    break;
            }
        }

        #endregion

        #region IPeterPluginTab Members

        public void Save()
        {
        }

        public void SaveAs(string filePath)
        {
        }

        public void Cut()
        {
        }

        public void Copy()
        {
        }

        public void Paste()
        {
        }

        public void Undo()
        {
        }

        public void Redo()
        {
        }

        public void Delete()
        {
        }

        public void Duplicate()
        {
        }

        public void Print()
        {
        }

        public void SelectAll()
        {
        }

        public bool CloseTab()
        {
            this.Close();
            return true;
        }

        public void MarkAll(System.Text.RegularExpressions.Regex reg)
        {
        }

        public bool FindNext(System.Text.RegularExpressions.Regex reg, bool searchUp)
        {
            return false;
        }

        public void ReplaceNext(System.Text.RegularExpressions.Regex reg, string replaceWith, bool searchUp)
        {
        }

        public void ReplaceAll(System.Text.RegularExpressions.Regex reg, string replaceWith)
        {
        }

        public void SelectWord(int line, int offset, int wordLeng)
        {
        }

        public IPeterPluginHost Host
        {
            get { return this.m_Host; }

            set { this.m_Host = value; }
        }

        public string FileName
        {
            get { return ""; }
        }

        public string Selection
        {
            get { return ""; }
        }

        public bool AbleToUndo
        {
            get { return false; }
        }

        public bool AbleToRedo
        {
            get { return false; }
        }

        public bool NeedsSaving
        {
            get { return false; }
        }

        public bool AbleToPaste
        {
            get { return false; }
        }

        public bool AbleToCut
        {
            get { return false; }
        }

        public bool AbleToCopy
        {
            get { return false; }
        }

        public bool AbleToSelectAll
        {
            get { return false; }
        }

        public bool AbleToSave
        {
            get { return false; }
        }

        public bool AbleToDelete
        {
            get { return false; }
        }

        #endregion

        #region -= Tool Bar =-

        private void tsbExpandAll_Click(object sender, EventArgs e)
        {
            for (int a = 0; a < this.treeMain.Nodes.Count; a++)
            {
                this.treeMain.Nodes[a].ExpandAll();
            }
        }

        private void tsbCollapseAll_Click(object sender, EventArgs e)
        {
            for (int a = 0; a < this.treeMain.Nodes.Count; a++)
            {
                this.treeMain.Nodes[a].Collapse();
            }
        }

        #endregion
        
        private void ParseCode ()
        {
            this.Clear();

            if (this.m_Editor != null)
            {
                if (!string.IsNullOrEmpty(this.m_Editor.FileName))
                {
                    string ext = Path.GetExtension(this.m_Editor.FileName).ToLower();
                    this.SetImageList(ext);
                    this.m_CanScroll = true;

                    switch (ext)
                    {
                        /*.manifest;.config;.addin;.wxs;.wxi;.wxl;.proj;
                         * .ilproj;.booproj;.build;.xfrm;.targets;
                         * .xpt;.xft;.map;.wsdl;.disco*/
                        case ".d":
                            DParser.DParser.ParseToTree(this.m_Editor.FileName, this.TreeNodes());
                           


                            break;
                        case ".xml":
                        case ".xshd":
                        case ".csproj":
                        case ".vbproj":
                        case ".xaml":
                        case ".xsl":
                        case ".xslt":
                        case ".xsd":
                        case ".jpx":
                            this.m_CanScroll = false;
                            XmlParser.ParseToTree(this.m_Editor.FileName, this.TreeNodes());
                            break;
                        case ".cs":
                            CSParser.CSParser.ParseToTree(this.m_Editor.FileName, this.TreeNodes());
                            break;
                        case ".css":
                            this.m_CanScroll = false;
                            CSSParser.CSSParser.ParseToTree(this.m_Editor.FileName, this.TreeNodes());
                            break;
                        case ".java":
                            JavaParser.JavaParser.ParseToTree(this.m_Editor.FileName, this.TreeNodes());
                            break;
                        case ".c":
                            CParser.CParser.ParseToTree(this.m_Editor.FileName, this.TreeNodes());
                            break;
                        case ".h":
                            CParser.CParser.ParseToTree(this.m_Editor.FileName, this.TreeNodes());
                            break;
                    }
                }
            }
        }

        
    }
}
