using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using ReactiveUI;
using System;
using System.Collections;
using System.Reactive;

namespace AvaloniaTabbedApp.Views;

public partial class MainWindow : AppWindow
{
    private bool AddNewTab;

    public MainWindow(bool addNewTab = true)
    {
        InitializeComponent();
        AddNewTab = addNewTab;
    }

    public static readonly string DataIdentifier = "FluentAvaloniaTabViewItem";

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (TitleBar != null)
        {
            TitleBar.ExtendsContentIntoTitleBar = true;
            TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

            var dragRegion = this.FindControl<Panel>("CustomDragRegion");
            dragRegion.MinWidth = FlowDirection == Avalonia.Media.FlowDirection.LeftToRight ?
                TitleBar.RightInset : TitleBar.LeftInset;

        }
    }

    public static TabViewItem CreateNewTab(bool selectTab)
    {
        return new TabViewItem()
        {
            IsClosable = true,
            IsSelected = selectTab,
            IconSource = new SymbolIconSource() { FontSize = 16, Symbol = Symbol.Home },
            Header = "Home",
            Content = new MainView(),
        };
    }

    private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (AddNewTab)
        {
            ((IList)tabView.TabItems).Add(CreateNewTab(true));
        }
    }

    private void TabView_AddTabButtonClick(FluentAvalonia.UI.Controls.TabView sender, System.EventArgs args)
    {
        ((IList)sender.TabItems).Add(CreateNewTab(true));
    }

    private void TabView_TabCloseRequested(FluentAvalonia.UI.Controls.TabView sender, FluentAvalonia.UI.Controls.TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab.IsClosable)
        {
            var tabItems = (IList)sender.TabItems;
            if (tabItems.Count <= 1)
            {
                this.Close();
            }
            else
            {
                tabItems.Remove(args.Tab);
            }
        }
    }

    private void TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        // Set the data payload to the drag args
        args.Data.SetData(DataIdentifier, args.Tab);

        // Indicate we can move
        args.Data.RequestedOperation = DragDropEffects.Move;
    }

    private void TabStripDrop(object sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataIdentifier) && e.Data.Get(DataIdentifier) is TabViewItem tvi)
        {
            var destinationTabView = sender as TabView;

            // While the TabView's internal ListView handles placing an insertion point gap, it 
            // doesn't actually hold that position upon drop - meaning you now must calculate
            // the approximate position of where to insert the tab
            int index = -1;

            for (int i = 0; i < destinationTabView.TabItems.Count(); i++)
            {
                var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;

                if (e.GetPosition(item).X - item.Bounds.Width < 0)
                {
                    index = i;
                    break;
                }
            }

            // Now remove the item from the source TabView
            var srcTabView = tvi.FindAncestorOfType<TabView>();
            var srcIndex = srcTabView.IndexFromContainer(tvi);
            (srcTabView.TabItems as IList).RemoveAt(srcIndex);

            // Now add it to the new TabView
            if (index < 0)
            {
                (destinationTabView.TabItems as IList).Add(tvi);
            }
            else if (index < destinationTabView.TabItems.Count())
            {
                (destinationTabView.TabItems as IList).Insert(index, tvi);
            }

            destinationTabView.SelectedItem = tvi;
            e.Handled = true;

            // TabItemsChanged won't fire during DragDrop so we need to check
            // here if we should close the window if TabItems.Count() == 0
            if (srcTabView.TabItems.Count() == 0)
            {
                var wnd = srcTabView.FindAncestorOfType<AppWindow>();
                wnd.Close();
            }
        }
    }

    private void TabStripDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataIdentifier))
        {
            // For dragover, use the standard DragEffects property
            e.DragEffects = DragDropEffects.Move;
        }
    }

    private void TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
    {
        if (sender.TabItems.Count() <= 1)
        {
            // Don't detach tab if it's the only tab in the window.
            return;
        }

        // In this case, the tab was dropped outside of any tabstrip, let's move it to
        // a new window
        var s = new MainWindow(addNewTab: false);

        // TabItems is by default initialized to an AvaloniaList<object>, so we can just
        // cast to IList and add
        // Be sure to remove the tab item from it's old TabView FIRST or else you'll get the
        // annoying "Item already has a Visual parent error"
        if (s.tabView.TabItems is IList l)
        {
            // If you're binding, args also as 'Item' where you can retrieve the data item instead
            (sender.TabItems as IList).Remove(args.Tab);

            // Preserving tab content state is easiest if you aren't binding. If you are, you will
            // need to manage preserving the state of the tabcontent across the different TabViews
            l.Add(args.Tab);
        }

        s.Show();
    }

    private void MainWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs args)
    {
        // NOTE: Ctrl + F4 for closing tabs and Ctrl + (Shift +)Tab for switching tab are already implemented in the TabView control itself
        if (args.KeyModifiers == KeyModifiers.Control)
        {
            int tabToSelect = -1;
            switch (args.Key)
            {
                case Key.T:
                    ((IList)tabView.TabItems).Add(CreateNewTab(true));
                    args.Handled = true;
                    return;
                case Key.W:
                    if (tabView.SelectedItem is TabViewItem tabViewItem && tabViewItem.IsClosable)
                    {
                        (tabView.TabItems as IList)?.Remove(tabView.SelectedItem);
                    }
                    args.Handled = true;
                    return;
                case Key.D1:
                case Key.NumPad1:
                    tabToSelect = 0;
                    break;
                case Key.D2:
                case Key.NumPad2:
                    tabToSelect = 1;
                    break;
                case Key.D3:
                case Key.NumPad3:
                    tabToSelect = 2;
                    break;
                case Key.D4:
                case Key.NumPad4:
                    tabToSelect = 3;
                    break;
                case Key.D5:
                case Key.NumPad5:
                    tabToSelect = 4;
                    break;
                case Key.D6:
                case Key.NumPad6:
                    tabToSelect = 5;
                    break;
                case Key.D7:
                case Key.NumPad7:
                    tabToSelect = 6;
                    break;
                case Key.D8:
                case Key.NumPad8:
                    tabToSelect = 7;
                    break;
                case Key.D9:
                case Key.NumPad9:
                    tabToSelect = tabView.TabItems.Count() - 1; ;
                    break;
            }
            if (tabToSelect >= 0 && tabToSelect < tabView.TabItems.Count())
            {
                tabView.SelectedIndex = tabToSelect;
                args.Handled = true;
            }
        }
    }
}
