using System.Linq;
using System.Windows;

namespace KronyxEditor.Docking
{
    // 플로팅 탭 전용 임시 호스트 창.
    public partial class FloatingDockWindow : Window
    {
        private readonly DockWorkspace _workspace;
        public DockPaneNode Root { get; }

        public FloatingDockWindow(DockLayoutManager manager, DockPaneNode root)
        {
            InitializeComponent();
            Root = root;
            _workspace = new DockWorkspace(new DockLayoutManager(root));
            RootGrid.Children.Add(_workspace);

            // 창이 닫힐 때 남아있는 탭을 메인 레이아웃으로 되돌린다.
            Closed += (s, e) =>
            {
                foreach (var pane in _workspace.Manager.GetPanes())
                {
                    foreach (var item in pane.Items)
                    {
                        item.IsFloating = false;
                        var target = manager.GetPanes().FirstOrDefault();
                        if (target != null)
                        {
                            target.Items.Add(item);
                            target.ActiveItemId = item.Id;
                        }
                    }
                }
                manager.CleanupEmptyPanes();
            };
        }
    }
}
