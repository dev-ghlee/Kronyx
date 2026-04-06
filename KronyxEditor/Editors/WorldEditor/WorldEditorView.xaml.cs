using KronyxEditor.Docking;
using KronyxEditor.GameProject;
using KronyxEditor.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KronyxEditor.Editors
{
    public partial class WorldEditorView : UserControl
    {
        // 도킹 레이아웃 저장 파일 경로.
        private readonly string _layoutFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DockLayout.json");
        private DockLayoutManager _layoutManager;
        private DockWorkspace _workspace;

        public WorldEditorView()
        {
            InitializeComponent();
            Loaded += OnWorldEditorViewLoaded;
            Focusable = true;
            BuildDefaultLayout();
        }

        // 기본 패널 구성 + 기본 Split 트리 생성.
        private void BuildDefaultLayout()
        {
            // 기존 에디터 패널을 도킹 시스템에 연결.
            DockContentRegistry.Register("SceneHierarchy", () => new ProjectLayoutView());
            DockContentRegistry.Register("Inspector", () => new GameEntityView());
            DockContentRegistry.Register("UndoRedo", () => new UndoRedoView { DataContext = Project.UndoRedo });

            // 초기 레이아웃:
            // 좌측 Scene / 우측 상단 History / 우측 하단 Inspector
            var leftPane = new DockPaneNode();
            leftPane.Items.Add(new DockItemModel { Title = "Scene", ContentId = "SceneHierarchy" });
            leftPane.ActiveItemId = leftPane.Items[0].Id;

            var bottomRightPane = new DockPaneNode();
            bottomRightPane.Items.Add(new DockItemModel { Title = "Inspector", ContentId = "Inspector" });
            bottomRightPane.ActiveItemId = bottomRightPane.Items[0].Id;

            var topRightPane = new DockPaneNode();
            topRightPane.Items.Add(new DockItemModel { Title = "History", ContentId = "UndoRedo" });
            topRightPane.ActiveItemId = topRightPane.Items[0].Id;

            var rightSplit = new DockSplitNode
            {
                Orientation = DockSplitOrientation.Vertical,
                Ratio = 0.5,
                First = topRightPane,
                Second = bottomRightPane
            };

            var root = new DockSplitNode
            {
                Orientation = DockSplitOrientation.Horizontal,
                Ratio = 0.55,
                First = leftPane,
                Second = rightSplit
            };

            _layoutManager = new DockLayoutManager(root);
            _workspace = new DockWorkspace(_layoutManager);
            DockWorkspaceHost.Children.Clear();
            DockWorkspaceHost.Children.Add(_workspace);
        }

        // 최초 로드 시 저장된 레이아웃이 있으면 복원.
        private void OnWorldEditorViewLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnWorldEditorViewLoaded;
            Keyboard.Focus(this);

            // Ensure UndoRedo panel receives latest DataContext after project load.
            DockContentRegistry.Register("UndoRedo", () => new UndoRedoView { DataContext = Project.UndoRedo });

            if (File.Exists(_layoutFile))
            {
                _layoutManager.LoadFromFile(_layoutFile, ResetFloatingWindows);
                _workspace.Refresh();
            }
        }

        // 기존 플로팅 창을 모두 닫고 컬렉션 정리.
        private void ResetFloatingWindows()
        {
            foreach (var window in _layoutManager.FloatingWindows.ToArray())
            {
                window.Close();
            }
            _layoutManager.FloatingWindows.Clear();
        }

        // 현재 레이아웃을 JSON으로 저장.
        private void OnSaveLayoutClick(object sender, RoutedEventArgs e)
        {
            _layoutManager.SaveToFile(_layoutFile);
        }

        // JSON 레이아웃을 다시 로드.
        private void OnLoadLayoutClick(object sender, RoutedEventArgs e)
        {
            _layoutManager.LoadFromFile(_layoutFile, ResetFloatingWindows);
            _workspace.Refresh();
        }
    }
}
