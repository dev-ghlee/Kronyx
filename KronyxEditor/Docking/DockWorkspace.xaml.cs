using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace KronyxEditor.Docking
{
    // DockNode 트리를 실제 WPF UI로 렌더링하고, 드래그/드롭 입력을 처리하는 뷰.
    public partial class DockWorkspace : UserControl
    {
        public DockLayoutManager Manager { get; }

        private DockPaneNode _hoverPane;
        private DockZone _hoverZone = DockZone.Center;

        public DockWorkspace(DockLayoutManager manager)
        {
            InitializeComponent();
            Manager = manager;
            AllowDrop = true;
            DragOver += OnWorkspaceDragOver;
            Drop += OnWorkspaceDrop;
            DragLeave += OnWorkspaceDragLeave;
            Refresh();
        }

        // 레이아웃 트리를 다시 그린다.
        public void Refresh()
        {
            LayoutHost.Content = BuildNode(Manager.Root);
        }

        // 노드 타입에 따라 Split 또는 Pane 컨트롤을 재귀적으로 생성.
        private UIElement BuildNode(DockNode node)
        {
            if (node is DockPaneNode pane)
            {
                return BuildPane(pane);
            }

            var split = node as DockSplitNode;
            var grid = new Grid { Margin = new Thickness(1) };

            if (split.Orientation == DockSplitOrientation.Horizontal)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(split.Ratio, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0 - split.Ratio, GridUnitType.Star) });

                var left = BuildNode(split.First);
                var right = BuildNode(split.Second);
                var splitter = new GridSplitter
                {
                    Width = 4,
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                Grid.SetColumn(left, 0);
                Grid.SetColumn(splitter, 1);
                Grid.SetColumn(right, 2);

                grid.Children.Add(left);
                grid.Children.Add(splitter);
                grid.Children.Add(right);
            }
            else
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(split.Ratio, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0 - split.Ratio, GridUnitType.Star) });

                var top = BuildNode(split.First);
                var bottom = BuildNode(split.Second);
                var splitter = new GridSplitter
                {
                    Height = 4,
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                Grid.SetRow(top, 0);
                Grid.SetRow(splitter, 1);
                Grid.SetRow(bottom, 2);

                grid.Children.Add(top);
                grid.Children.Add(splitter);
                grid.Children.Add(bottom);
            }

            return grid;
        }

        // 하나의 Pane(탭 그룹) 렌더링.
        private UIElement BuildPane(DockPaneNode pane)
        {
            pane.EnsureActive();

            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(1)
            };

            var dock = new DockPanel();
            border.Child = dock;

            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 2, 0)
            };
            DockPanel.SetDock(toolbar, Dock.Top);

            var autoHideButton = new Button
            {
                Content = pane.IsAutoHide ? "📌" : "📍",
                ToolTip = "Toggle Auto Hide",
                Padding = new Thickness(4, 0, 4, 0),
                Margin = new Thickness(2)
            };
            autoHideButton.Click += (s, e) =>
            {
                Manager.ToggleAutoHide(pane);
                Refresh();
            };

            toolbar.Children.Add(autoHideButton);
            dock.Children.Add(toolbar);

            var tabControl = new TabControl
            {
                Tag = pane,
                AllowDrop = true
            };

            tabControl.SelectionChanged += (s, e) =>
            {
                if (tabControl.SelectedItem is TabItem ti && ti.Tag is DockItemModel item)
                {
                    pane.ActiveItemId = item.Id;
                }
            };

            tabControl.DragOver += OnPaneDragOver;
            tabControl.Drop += OnPaneDrop;
            tabControl.DragLeave += OnPaneDragLeave;

            // Pane의 탭 헤더 + float 버튼 생성.
            foreach (var item in pane.Items)
            {
                var tab = new TabItem { Tag = item };
                tab.Header = CreateTabHeader(pane, item, tabControl);
                var contentElement = item.CreateContent();
                if (pane.IsAutoHide && pane.ActiveItemId != item.Id)
                {
                    contentElement.Visibility = Visibility.Collapsed;
                }
                tab.Content = contentElement;
                tabControl.Items.Add(tab);

                if (pane.ActiveItemId == item.Id)
                {
                    tabControl.SelectedItem = tab;
                }
            }

            dock.Children.Add(tabControl);
            return border;
        }

        // 탭 헤더(제목 + float 버튼 + 드래그 시작 이벤트) 생성.
        private UIElement CreateTabHeader(DockPaneNode pane, DockItemModel item, TabControl owner)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var text = new TextBlock
            {
                Text = item.Title,
                Margin = new Thickness(6, 2, 6, 2)
            };
            Grid.SetColumn(text, 0);

            var floatBtn = new Button
            {
                Content = "↗",
                FontSize = 10,
                Padding = new Thickness(2, 0, 2, 0),
                Margin = new Thickness(0, 0, 3, 0),
                ToolTip = "Float"
            };
            floatBtn.Click += (s, e) =>
            {
                e.Handled = true;
                FloatItem(pane, item);
            };
            Grid.SetColumn(floatBtn, 1);

            grid.Children.Add(text);
            grid.Children.Add(floatBtn);

            grid.MouseMove += (s, e) =>
            {
                if (e.LeftButton != MouseButtonState.Pressed) return;
                var payload = new DockDragPayload { SourcePane = pane, Item = item };
                DragDrop.DoDragDrop(grid, payload, DragDropEffects.Move);
            };

            var contextMenu = new ContextMenu();
            var dockBack = new MenuItem { Header = "Dock To Main" };
            dockBack.Click += (s, e) => DockToMain(pane, item);
            contextMenu.Items.Add(dockBack);
            grid.ContextMenu = contextMenu;

            return grid;
        }

        // 탭을 분리해 플로팅 윈도우로 띄운다.
        private void FloatItem(DockPaneNode sourcePane, DockItemModel item)
        {
            Manager.RemoveItem(sourcePane, item);
            item.IsFloating = true;

            var pane = new DockPaneNode();
            pane.Items.Add(item);
            pane.ActiveItemId = item.Id;

            var window = new FloatingDockWindow(Manager, pane);
            Manager.FloatingWindows.Add(window);
            window.Closed += (s, e) => Manager.FloatingWindows.Remove(window);
            window.Show();

            Refresh();
        }

        // 플로팅 탭을 메인 레이아웃 첫 Pane로 복귀.
        private void DockToMain(DockPaneNode sourcePane, DockItemModel item)
        {
            var targetPane = Manager.GetPanes().FirstOrDefault();
            if (targetPane == null) return;

            item.IsFloating = false;
            Manager.MoveItem(sourcePane, targetPane, item, DockZone.Center);
            Refresh();
        }

        // 워크스페이스 전역 DragOver: 드롭 가능 시 가이드 표시.
        private void OnWorkspaceDragOver(object sender, DragEventArgs e)
        {
            if (!(e.Data.GetData(typeof(DockDragPayload)) is DockDragPayload)) return;

            DropOverlay.Visibility = Visibility.Visible;
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        // 워크스페이스 전역 Drop: 기본적으로 첫 Pane에 탭으로 합침.
        private void OnWorkspaceDrop(object sender, DragEventArgs e)
        {
            HideDropPreview();
            if (!(e.Data.GetData(typeof(DockDragPayload)) is DockDragPayload payload)) return;

            var target = Manager.GetPanes().FirstOrDefault();
            if (target == null) return;

            Manager.MoveItem(payload.SourcePane, target, payload.Item, DockZone.Center);
            Refresh();
        }

        private void OnWorkspaceDragLeave(object sender, DragEventArgs e)
        {
            HideDropPreview();
        }

        // Pane 내부 DragOver: 마우스 위치로 도킹 영역을 계산.
        private void OnPaneDragOver(object sender, DragEventArgs e)
        {
            if (!(e.Data.GetData(typeof(DockDragPayload)) is DockDragPayload payload)) return;

            var tabControl = sender as TabControl;
            var pane = tabControl?.Tag as DockPaneNode;
            if (pane == null) return;

            var point = e.GetPosition(tabControl);
            _hoverPane = pane;
            _hoverZone = GetDockZone(point, tabControl.ActualWidth, tabControl.ActualHeight);

            ShowDropPreview(tabControl, _hoverZone);
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        // Pane Drop: 계산된 영역(Left/Right/Top/Bottom/Center)에 반영.
        private void OnPaneDrop(object sender, DragEventArgs e)
        {
            HideDropPreview();
            if (!(e.Data.GetData(typeof(DockDragPayload)) is DockDragPayload payload) || _hoverPane == null) return;

            var tabControl = sender as TabControl;
            var targetIndex = GetDropTabIndex(tabControl, e.GetPosition(tabControl));

            Manager.MoveItem(payload.SourcePane, _hoverPane, payload.Item, _hoverZone, targetIndex);
            Refresh();
        }

        private void OnPaneDragLeave(object sender, DragEventArgs e)
        {
            HideDropPreview();
        }

        // 탭 헤더 위에 드롭했는지 검사해 삽입 인덱스를 결정.
        private int GetDropTabIndex(TabControl tabControl, Point point)
        {
            for (var i = 0; i < tabControl.Items.Count; i++)
            {
                if (!(tabControl.ItemContainerGenerator.ContainerFromIndex(i) is TabItem tab)) continue;

                var bounds = VisualTreeHelper.GetDescendantBounds(tab);
                var topLeft = tab.TranslatePoint(bounds.TopLeft, tabControl);
                var rect = new Rect(topLeft, bounds.Size);
                if (rect.Contains(point)) return i;
            }

            return -1;
        }

        // 마우스 상대 좌표를 5분할 도킹 영역으로 변환.
        private DockZone GetDockZone(Point point, double width, double height)
        {
            if (width <= 1 || height <= 1) return DockZone.Center;

            var x = point.X / width;
            var y = point.Y / height;

            if (x < 0.25) return DockZone.Left;
            if (x > 0.75) return DockZone.Right;
            if (y < 0.25) return DockZone.Top;
            if (y > 0.75) return DockZone.Bottom;
            return DockZone.Center;
        }

        // 현재 zone에 맞는 프리뷰 사각형 표시.
        private void ShowDropPreview(FrameworkElement target, DockZone zone)
        {
            DropOverlay.Visibility = Visibility.Visible;

            var bounds = new Rect(0, 0, target.ActualWidth, target.ActualHeight);
            Rect preview;
            switch (zone)
            {
                case DockZone.Left:
                    preview = new Rect(bounds.Left, bounds.Top, bounds.Width * 0.4, bounds.Height);
                    break;
                case DockZone.Right:
                    preview = new Rect(bounds.Left + bounds.Width * 0.6, bounds.Top, bounds.Width * 0.4, bounds.Height);
                    break;
                case DockZone.Top:
                    preview = new Rect(bounds.Left, bounds.Top, bounds.Width, bounds.Height * 0.4);
                    break;
                case DockZone.Bottom:
                    preview = new Rect(bounds.Left, bounds.Top + bounds.Height * 0.6, bounds.Width, bounds.Height * 0.4);
                    break;
                default:
                    preview = new Rect(bounds.Left + bounds.Width * 0.2, bounds.Top + bounds.Height * 0.2, bounds.Width * 0.6, bounds.Height * 0.6);
                    break;
            }

            var translated = target.TranslatePoint(new Point(preview.Left, preview.Top), this);
            PreviewOverlay.Margin = new Thickness(translated.X, translated.Y, 0, 0);
            PreviewOverlay.Width = preview.Width;
            PreviewOverlay.Height = preview.Height;
            PreviewOverlay.HorizontalAlignment = HorizontalAlignment.Left;
            PreviewOverlay.VerticalAlignment = VerticalAlignment.Top;
            PreviewOverlay.Visibility = Visibility.Visible;
        }

        // 도킹 가이드/프리뷰 숨김.
        private void HideDropPreview()
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            PreviewOverlay.Visibility = Visibility.Collapsed;
            _hoverPane = null;
            _hoverZone = DockZone.Center;
        }
    }
}
