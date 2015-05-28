﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace GrayStorm
{
    public partial class hierarchyViewer : UserControl
    {
        #region events
        public delegate void targetMethod(MethodInfo targetMethod, TreeNode TN);
        public event targetMethod selectedMethod;

        public delegate void targetConstructor(ConstructorInfo targetConstructor, TreeNode TN);
        public event targetConstructor selectedConstructor;

        public hierarchyViewer()
        {
            InitializeComponent();
        }

        void hierarchyViewer_selectedMethod(MethodInfo targetMethod, TreeNode TN)
        {
            domainTraverser.currentMethod = targetMethod;
            GrayStorm.grayStorm._methodLabel_TB.Text = TN.Parent.Text + "." + targetMethod.Name;

        }

        void hierarchyViewer_selectedConstructor(ConstructorInfo targetConstructor, TreeNode TN)
        {
            domainTraverser.currentConstructor = targetConstructor;
            GrayStorm.grayStorm._constructorLabel_TB.Text = TN.Parent.Text + "." + TN.Text;
        }

        private void refreshDomain_BT_Click(object sender, EventArgs e)
        {
            domainAssemblies.Clear();
            domainClasses.Clear();
            methods.Clear();
            constructors.Clear();
            hierarchyViewer_TN.Nodes.Clear();
            buildTree();
        }

        #endregion events

        #region init
        public void loadhierarchyViewer()
        {
            selectedMethod += hierarchyViewer_selectedMethod;
            selectedConstructor += hierarchyViewer_selectedConstructor;
            buildTree();
        }


        #endregion init

        #region treeNode
        System.Collections.Generic.Dictionary<TreeNode, Assembly> domainAssemblies = new Dictionary<TreeNode, Assembly>();
        System.Collections.Generic.Dictionary<TreeNode, Type> domainClasses = new Dictionary<TreeNode, Type>();
        System.Collections.Generic.Dictionary<TreeNode, MethodInfo> methods = new Dictionary<TreeNode, MethodInfo>();
        System.Collections.Generic.Dictionary<TreeNode, ConstructorInfo> constructors = new Dictionary<TreeNode, ConstructorInfo>();

        public void buildTree()
        {
            AppDomain myDomain = AppDomain.CurrentDomain;
            Assembly[] allAssemblies = myDomain.GetAssemblies();

            foreach (Assembly asm in allAssemblies)
            {
                string assemblyName = asm.FullName.ToString();
                System.Windows.Forms.TreeNode Assemblies = new TreeNode(assemblyName.ToString());
                domainAssemblies.Add(Assemblies, asm);
                hierarchyViewer_TN.Nodes.Add(Assemblies);
            }
            hierarchyViewer_TN.Sort();
        }
        private void hierarchyViewer_TN_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode theSelectedNode = hierarchyViewer_TN.SelectedNode;

            if (theSelectedNode != null)
            {
                if (domainAssemblies.ContainsKey(theSelectedNode))
                {
                    Assembly assemblySelected = domainAssemblies[theSelectedNode];
                    

                    Type[] types = assemblySelected.GetTypes();
                    foreach (Type type in types)
                    {
                        System.Windows.Forms.TreeNode asmClass = new TreeNode(type.ToString());
                        domainClasses.Add(asmClass, type);
                        theSelectedNode.Nodes.Add(asmClass);
                        makeMethodandFunctionList(asmClass);
                    }
                    hierarchyViewer_TN.Sort();
                }
                else if (methods.ContainsKey(theSelectedNode))
                {
                    MethodInfo methodSelected = methods[theSelectedNode];

                    if (selectedMethod != null && methodSelected != null)
                        selectedMethod(methodSelected, theSelectedNode);
                }
                else if (constructors.ContainsKey(theSelectedNode))
                {
                    ConstructorInfo constructorSelected = constructors[theSelectedNode];

                    Assembly assemblySelected = domainAssemblies[theSelectedNode.Parent.Parent];
                    domainTraverser.assemblyInfo = assemblySelected;
                    domainTraverser.typeInfo = assemblySelected.GetType(theSelectedNode.Parent.Text);


                    if (selectedConstructor != null && constructorSelected != null)
                        selectedConstructor(constructorSelected, theSelectedNode);
                }
            }
        }

        private void makeMethodandFunctionList(TreeNode parent)
        {
            try
            {
                var classSelected = domainClasses[parent];

                MethodInfo[] methodInfo = classSelected.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy);
                ConstructorInfo[] constructorList = classSelected.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (MethodInfo classMethods in methodInfo)
                {
                    System.Windows.Forms.TreeNode asmMethod = new TreeNode(classMethods.Name);
                    parent.Nodes.Add(asmMethod);
                    methods.Add(asmMethod, classMethods);
                }

                foreach (ConstructorInfo constructorInfo in constructorList)
                {
                    object fullConType = constructorInfo as object;
                    System.Windows.Forms.TreeNode asmMethod = new TreeNode(fullConType.ToString());
                    parent.Nodes.Add(asmMethod);
                    constructors.Add(asmMethod, constructorInfo);
                }
            }
            catch { }
        }
        #endregion

        #region rightClickMenu

        private void dumpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grayStorm._memoryHijacker.setDisassembleUnchecked();
            grayStorm._memoryHijacker.dumpAsm_BT_Click(null, null);
        }

        private void disassembleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grayStorm._memoryHijacker.setDisassembleChecked();
            grayStorm._memoryHijacker.dumpAsm_BT_Click(null, null);
        }

        private void reDumpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int containedIndex = getContainedIndex();
            if (containedIndex > 0)
            {
                methodHelpers.StorageInformationArrayList[containedIndex].dumped = false;
                grayStorm._memoryHijacker.dumpAsm_BT_Click(null, null);
            }
        }

        private void restoreMethodToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int containedIndex = getContainedIndex();
            if (containedIndex == -1)
                return;
            try
            {
                GrayStorm.assemblyHelpers.holder = methodHelpers.StorageInformationArrayList[containedIndex].oldMethod;
                methodHijacking.writeAMethod(methodHelpers.StorageInformationArrayList[containedIndex].methodIntPtr);
                methodHelpers.StorageInformationArrayList[containedIndex].dumped = false;
            }
            catch { }
        }

        private void showILCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grayStorm._memoryHijacker.getIL_BT_Click(null, null);
        }

        private void replaceMethodWithCustomCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MethodInfo replacement = grayStorm._memoryHijacker.dynamicMethods_LB.SelectedItem as MethodInfo;
            if (replacement == null || domainTraverser.currentMethod == null)
                return;
            dynamic_C.methodReplacer.replaceIL(domainTraverser.currentMethod, replacement);
        }

        int savedCachePtr = -1;
        private void cacheReplacerMethodToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int containedIndex = getContainedIndex();
            if (containedIndex == -1)
            {
                grayStorm._memoryHijacker.dumpAsm_BT_Click(null, null);
                containedIndex = methodHelpers.containedInList(domainTraverser.currentMethod);
            }
            savedCachePtr = containedIndex;
        }

        private int getContainedIndex()
        {
            if (domainTraverser.currentMethod != null)
                return methodHelpers.containedInList(domainTraverser.currentMethod);
            else return -1;
        }

        #endregion rightClickMenu
    }
}
    
       



    
