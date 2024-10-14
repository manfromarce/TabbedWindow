using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUiTabbedApp;

/// <summary>
/// Main window
/// </summary>
public sealed partial class MainWindow : Window
{
    private const string DataIdentifier = "WinUiTabViewItem";

    public MainWindow(bool addNewTab = true)
    {
        this.InitializeComponent();
        
        tabView.Tag = this;
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(CustomDragRegion);
        CustomDragRegion.MinWidth = 188;
        if (addNewTab)
        {
            AddTab();
        }
    }

    public void AddTab(TabViewItem? tab = null, bool selectNewTab = true)
    {
        if (tab == null)
        {
            tab = CreateTab();
        }
        tab.IsSelected = selectNewTab;
        tabView.TabItems.Add(tab);
        tabView.SelectedItem = tab;
    }

    public TabViewItem CreateTab(string header = "Home")
    {
        return new TabViewItem()
        {
            Header = header,
            Content = new MainView()
        };
    }

    private void Tabs_AddTabButtonClick(TabView sender, object args)
    {
        var tab = CreateTab();
        tab.IsSelected = true;
        sender.TabItems.Add(tab);
        sender.SelectedItem = tab;
    }

    private void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        // Only close the selected tab if it is closeable
        if (args.Tab.IsClosable)
        {
            sender.TabItems.Remove(args.Tab);
        }
    }

    private void Tabs_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
    {
        if (sender.TabItems.Count <= 1)
        {
            return;
        }

        sender.TabItems.Remove(args.Tab);

        var newWindow = new MainWindow(false);
        newWindow.Activate();
        newWindow.AddTab(args.Tab);
    }

    private void Tabs_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        var firstItem = args.Tab;
        args.Data.Properties.Add(DataIdentifier, firstItem);
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private async void Tabs_TabStripDrop(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue(DataIdentifier, out object obj))
        {
            if (obj == null)
            {
                return;
            }

            var destinationTabView = sender as TabView;
            var destinationItems = destinationTabView?.TabItems;

            if (destinationTabView != null && destinationItems != null)
            {
                // First we need to get the position in the List to drop to
                var index = -1;

                // Determine which items in the list our pointer is between.
                for (int i = 0; i < destinationTabView.TabItems.Count; i++)
                {
                    var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;

                    if (e.GetPosition(item).X - item?.ActualWidth < 0)
                    {
                        index = i;
                        break;
                    }
                }

                // The TabViewItem can only be in one tree at a time. Before moving it to the new TabView, remove it from the old.
                // Note that this call can happen on a different thread if moving across windows. So make sure you call methods on
                // the same thread as where the UI Elements were created.

                object? header = null;
                var element = (obj as UIElement);

                var taskCompletionSource = new TaskCompletionSource();

                element?.DispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Normal,
                    new DispatcherQueueHandler(() =>
                    {
                        var tabItem = obj as TabViewItem;
                        var sourceTabViewListView = (tabItem?.Parent as TabViewListView);
                        sourceTabViewListView?.Items.Remove(obj);
                        header = tabItem?.Header;
                        taskCompletionSource.SetResult();
                    }));

                await taskCompletionSource.Task;

                if (index < 0)
                {
                    // We didn't find a transition point, so we're at the end of the list
                    destinationItems.Add(obj);
                }
                else if (index < destinationTabView.TabItems.Count)
                {
                    // Otherwise, insert at the provided index.
                    destinationItems.Insert(index, obj);
                }

                // Select the newly dragged tab
                destinationTabView.SelectedItem = obj;
            }
        }
    }

    private void Tabs_TabStripDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey(DataIdentifier))
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }
    }

    private void Tabs_TabItemsChanged(TabView sender, IVectorChangedEventArgs args)
    {
        if (sender.TabItems.Count == 0)
        {
            ((Window)sender.Tag).Close();
        }
    }

    private void NewTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        AddTab();
        args.Handled = true;
    }

    private void CloseSelectedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var invokedTabView = (args.Element as TabView);
        // Only close the selected tab if it is closeable
        if (invokedTabView != null && ((TabViewItem)invokedTabView.SelectedItem).IsClosable)
        {
            invokedTabView.TabItems.Remove(invokedTabView.SelectedItem);
        }
        args.Handled = true;
    }

    private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var invokedTabView = (args.Element as TabView);
        if (invokedTabView != null)
        {
            int tabToSelect = 0;
            switch (sender.Key)
            {
                case Windows.System.VirtualKey.Number1:
                    tabToSelect = 0;
                    break;
                case Windows.System.VirtualKey.Number2:
                    tabToSelect = 1;
                    break;
                case Windows.System.VirtualKey.Number3:
                    tabToSelect = 2;
                    break;
                case Windows.System.VirtualKey.Number4:
                    tabToSelect = 3;
                    break;
                case Windows.System.VirtualKey.Number5:
                    tabToSelect = 4;
                    break;
                case Windows.System.VirtualKey.Number6:
                    tabToSelect = 5;
                    break;
                case Windows.System.VirtualKey.Number7:
                    tabToSelect = 6;
                    break;
                case Windows.System.VirtualKey.Number8:
                    tabToSelect = 7;
                    break;
                case Windows.System.VirtualKey.Number9:
                    // Select the last tab
                    tabToSelect = invokedTabView.TabItems.Count - 1;
                    break;
            }
            // Only select the tab if it is in the list
            if (tabToSelect < invokedTabView.TabItems.Count)
            {
                invokedTabView.SelectedIndex = tabToSelect;
            }
            args.Handled = true;
        }
    }
}
