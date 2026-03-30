using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TimeTableApp.Models;
using TimeTableApp.ViewModels;

namespace TimeTableApp.Behaviors
{
    /// <summary>
    /// Attached behaviour that adds drag-and-drop row reordering to an
    /// ItemsControl whose DataContext is a DayColumnViewModel.
    ///
    /// Usage in XAML:
    ///   behaviors:DragDropBehavior.IsEnabled="True"
    /// </summary>
    public static class DragDropBehavior
    {
        // ── Attached property ────────────────────────────────────────────────

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(DragDropBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
            => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value)
            => obj.SetValue(IsEnabledProperty, value);

        // ── State per-control (stored as tags on the element itself) ─────────

        private const string DragItemKey = "DDB_DragItem";
        private const string DragStartKey = "DDB_DragStart";
        private const string IsDraggingKey = "DDB_IsDragging";

        // ── Wiring ───────────────────────────────────────────────────────────

        private static void OnIsEnabledChanged(DependencyObject d,
                                               DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe) return;

            if ((bool)e.NewValue)
            {
                fe.PreviewMouseLeftButtonDown += OnMouseDown;
                fe.PreviewMouseMove += OnMouseMove;
                fe.PreviewMouseLeftButtonUp += OnMouseUp;
                fe.DragOver += OnDragOver;
                fe.Drop += OnDrop;
                fe.AllowDrop = true;
            }
            else
            {
                fe.PreviewMouseLeftButtonDown -= OnMouseDown;
                fe.PreviewMouseMove -= OnMouseMove;
                fe.PreviewMouseLeftButtonUp -= OnMouseUp;
                fe.DragOver -= OnDragOver;
                fe.Drop -= OnDrop;
                fe.AllowDrop = false;
            }
        }

        // ── Mouse down – record start position and item ──────────────────────

        private static void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement host) return;

            // Only start a drag from the drag-handle thumb
            var handle = FindAncestorByName(e.OriginalSource as DependencyObject, "DragHandle");
            if (handle == null) return;

            var item = GetItemFromSource(e.OriginalSource as DependencyObject, host);
            if (item == null) return;

            host.SetValue(DragItemFrameworkProperty, item);
            host.SetValue(DragStartFrameworkProperty, e.GetPosition(host));
            host.SetValue(IsDraggingFrameworkProperty, false);
            e.Handled = false;   // don't block text-box focus etc.
        }

        // ── Mouse move – initiate DragDrop once threshold is exceeded ────────

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not FrameworkElement host) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var dragItem = host.GetValue(DragItemFrameworkProperty) as DayTaskStatus;
            if (dragItem == null) return;

            var start = (Point)host.GetValue(DragStartFrameworkProperty);
            var current = e.GetPosition(host);
            var delta = current - start;

            if (Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance &&
                Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance)
                return;

            host.SetValue(IsDraggingFrameworkProperty, true);

            var data = new DataObject(typeof(DayTaskStatus), dragItem);
            DragDrop.DoDragDrop(host, data, DragDropEffects.Move);

            // Reset after drop completes
            host.SetValue(DragItemFrameworkProperty, null);
            host.SetValue(IsDraggingFrameworkProperty, false);
        }

        // ── Mouse up – cancel if dragging never started ───────────────────────

        private static void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement host) return;
            host.SetValue(DragItemFrameworkProperty, null);
            host.SetValue(IsDraggingFrameworkProperty, false);
        }

        // ── DragOver – provide visual feedback ───────────────────────────────

        private static void OnDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(DayTaskStatus)))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        // ── Drop – reorder inside the DayColumnViewModel ──────────────────────

        private static void OnDrop(object sender, DragEventArgs e)
        {
            if (sender is not FrameworkElement host) return;
            if (!e.Data.GetDataPresent(typeof(DayTaskStatus))) return;

            var dragged = (DayTaskStatus)e.Data.GetData(typeof(DayTaskStatus));
            var target = GetItemFromSource(e.OriginalSource as DependencyObject, host);

            if (target == null || ReferenceEquals(dragged, target)) return;

            // Resolve the DayColumnViewModel from the host's DataContext
            DayColumnViewModel? vm = ResolveViewModel(host);
            if (vm == null) return;

            int fromIdx = vm.DayTasks.IndexOf(dragged);
            int toIdx = vm.DayTasks.IndexOf(target);

            if (fromIdx < 0 || toIdx < 0) return;

            vm.DayTasks.Move(fromIdx, toIdx);
            vm.NotifyMoved();

            e.Handled = true;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// Walk the visual tree upward from a hit-test source to find
        /// the DayTaskStatus that belongs to the row under the cursor.
        private static DayTaskStatus? GetItemFromSource(DependencyObject? source,
                                                         FrameworkElement host)
        {
            var current = source;
            while (current != null && !ReferenceEquals(current, host))
            {
                if (current is FrameworkElement fe && fe.DataContext is DayTaskStatus item)
                    return item;
                current = VisualTreeHelper.GetParent(current)
                          ?? LogicalTreeHelper.GetParent(current);
            }
            return null;
        }

        /// Walk upward looking for an element with a given x:Name.
        private static FrameworkElement? FindAncestorByName(DependencyObject? source,
                                                              string name)
        {
            var current = source;
            while (current != null)
            {
                if (current is FrameworkElement fe && fe.Name == name)
                    return fe;
                current = VisualTreeHelper.GetParent(current)
                          ?? LogicalTreeHelper.GetParent(current);
            }
            return null;
        }

        private static DayColumnViewModel? ResolveViewModel(FrameworkElement host)
        {
            // The host is the ItemsControl; its DataContext is the DayColumnViewModel
            if (host.DataContext is DayColumnViewModel vm) return vm;

            // Walk up in case host is a child
            var parent = VisualTreeHelper.GetParent(host);
            while (parent != null)
            {
                if (parent is FrameworkElement fe && fe.DataContext is DayColumnViewModel pvm)
                    return pvm;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        // ── Private DependencyProperties stored on the host element ──────────
        // (using DependencyProperty.RegisterAttached for private state)

        private static readonly DependencyProperty DragItemFrameworkProperty =
            DependencyProperty.RegisterAttached("_DragItem", typeof(DayTaskStatus),
                typeof(DragDropBehavior), new PropertyMetadata(null));

        private static readonly DependencyProperty DragStartFrameworkProperty =
            DependencyProperty.RegisterAttached("_DragStart", typeof(Point),
                typeof(DragDropBehavior), new PropertyMetadata(new Point()));

        private static readonly DependencyProperty IsDraggingFrameworkProperty =
            DependencyProperty.RegisterAttached("_IsDragging", typeof(bool),
                typeof(DragDropBehavior), new PropertyMetadata(false));
    }
}