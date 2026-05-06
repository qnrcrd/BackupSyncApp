using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using System.Collections.Specialized;
using System.Linq;
namespace BackupSyncApp.Common
{
    public class AutoScrollBehavior: Behavior<System.Windows.Controls.ListBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            var items = AssociatedObject.Items;
            if (items is INotifyCollectionChanged collection) collection.CollectionChanged += OnCollectionChanged;

            AssociatedObject.Loaded += OnLoaded;
        }

        protected override void OnDetaching()
        {
            var items = AssociatedObject.Items;
            if (items is INotifyCollectionChanged collection) collection.CollectionChanged -= OnCollectionChanged;

            AssociatedObject.Loaded -= OnLoaded;
            base.OnDetaching();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ScrollToBottom();
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action==NotifyCollectionChangedAction.Add) ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (AssociatedObject.Items.Count > 0)
            {
                AssociatedObject.Dispatcher.BeginInvoke(() =>
                {
                    var lastItem = AssociatedObject.Items[AssociatedObject.Items.Count - 1];
                    AssociatedObject.ScrollIntoView(lastItem);
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }
}
