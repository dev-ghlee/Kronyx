using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace KronyxEditor.Docking
{
    // 드롭 시 패널을 배치할 수 있는 5개 영역.
    public enum DockZone
    {
        Left,
        Right,
        Top,
        Bottom,
        Center
    }

    // Split 노드가 자식을 나누는 방향.
    public enum DockSplitOrientation
    {
        Horizontal,
        Vertical
    }

    // 레이아웃 트리의 기본 노드 타입.
    public abstract class DockNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
    }

    // 화면을 두 영역으로 나누는 내부 노드.
    public class DockSplitNode : DockNode
    {
        public DockSplitOrientation Orientation { get; set; }
        public double Ratio { get; set; } = 0.5;
        public DockNode First { get; set; }
        public DockNode Second { get; set; }
    }

    // 탭 그룹(패널 묶음)을 담는 리프 노드.
    public class DockPaneNode : DockNode
    {
        public ObservableCollection<DockItemModel> Items { get; } = new ObservableCollection<DockItemModel>();
        public string ActiveItemId { get; set; }
        public bool IsAutoHide { get; set; }

        // 현재 활성 탭. ActiveItemId가 유효하지 않으면 첫 번째 탭을 대체 사용.
        public DockItemModel ActiveItem => Items.FirstOrDefault(x => x.Id == ActiveItemId) ?? Items.FirstOrDefault();

        // 활성 탭 ID가 비어 있거나 잘못된 경우를 복구.
        public void EnsureActive()
        {
            if (ActiveItem == null && Items.Count > 0)
            {
                ActiveItemId = Items[0].Id;
            }
        }
    }

    // 실제 패널(탭) 하나를 나타내는 모델.
    public class DockItemModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; }
        public string ContentId { get; set; }
        public bool IsFloating { get; set; }

        // ContentId를 기반으로 실제 WPF 컨트롤을 생성.
        public FrameworkElement CreateContent()
        {
            return DockContentRegistry.Create(ContentId);
        }
    }

    // 저장/복원 시 루트 스냅샷.
    public class DockLayoutSnapshot
    {
        public DockNodeDto Root { get; set; }
        public List<FloatingWindowSnapshot> FloatingWindows { get; set; } = new List<FloatingWindowSnapshot>();
    }

    // 플로팅 윈도우의 위치/크기 + 내부 레이아웃.
    public class FloatingWindowSnapshot
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public DockNodeDto Root { get; set; }
    }

    // DockNode를 JSON에 안전하게 저장하기 위한 DTO.
    public class DockNodeDto
    {
        public string NodeType { get; set; }
        public string Id { get; set; }
        public string Orientation { get; set; }
        public double Ratio { get; set; }
        public DockNodeDto First { get; set; }
        public DockNodeDto Second { get; set; }
        public List<DockItemDto> Items { get; set; }
        public string ActiveItemId { get; set; }
        public bool IsAutoHide { get; set; }
    }

    // DockItemModel 저장용 DTO.
    public class DockItemDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string ContentId { get; set; }
        public bool IsFloating { get; set; }
    }
}
