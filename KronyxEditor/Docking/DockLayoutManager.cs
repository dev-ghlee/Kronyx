using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace KronyxEditor.Docking
{
    // 도킹 트리 변경/정리/직렬화를 담당하는 핵심 매니저.
    public class DockLayoutManager
    {
        public DockNode Root { get; private set; }
        public ObservableCollection<FloatingDockWindow> FloatingWindows { get; } = new ObservableCollection<FloatingDockWindow>();

        public DockLayoutManager(DockNode root)
        {
            Root = root;
        }

        public IEnumerable<DockPaneNode> GetPanes()
        {
            return EnumeratePanes(Root);
        }

        public void SetRoot(DockNode root)
        {
            Root = root;
        }

        // fromPane의 item을 targetPane 기준 zone 위치로 이동.
        // - Center: 탭으로 합치기(필요 시 인덱스 삽입)
        // - 나머지: 새 Pane를 만들고 Split 생성
        public void MoveItem(DockPaneNode fromPane, DockPaneNode targetPane, DockItemModel item, DockZone zone, int targetIndex = -1)
        {
            if (fromPane == null || targetPane == null || item == null) return;

            fromPane.Items.Remove(item);
            fromPane.EnsureActive();

            if (zone == DockZone.Center)
            {
                if (targetIndex >= 0 && targetIndex <= targetPane.Items.Count)
                {
                    targetPane.Items.Insert(targetIndex, item);
                }
                else
                {
                    targetPane.Items.Add(item);
                }
                targetPane.ActiveItemId = item.Id;
            }
            else
            {
                var newPane = new DockPaneNode();
                newPane.Items.Add(item);
                newPane.ActiveItemId = item.Id;
                SplitPane(targetPane, newPane, zone);
            }

            CleanupEmptyPanes();
        }

        // targetPane 위치를 분할해서 newPane을 배치.
        public void SplitPane(DockPaneNode targetPane, DockPaneNode newPane, DockZone zone)
        {
            if (targetPane == null || newPane == null) return;

            var split = new DockSplitNode
            {
                Orientation = zone == DockZone.Left || zone == DockZone.Right
                    ? DockSplitOrientation.Horizontal
                    : DockSplitOrientation.Vertical,
                Ratio = 0.5
            };

            var placeFirst = zone == DockZone.Left || zone == DockZone.Top;
            split.First = placeFirst ? newPane : targetPane;
            split.Second = placeFirst ? targetPane : newPane;

            ReplaceNode(targetPane, split);
        }

        // Pane에서 탭 제거 후, 빈 노드 정리.
        public void RemoveItem(DockPaneNode pane, DockItemModel item)
        {
            pane?.Items.Remove(item);
            pane?.EnsureActive();
            CleanupEmptyPanes();
        }

        // 간단 auto-hide 토글(표시 정책은 뷰에서 처리).
        public void ToggleAutoHide(DockPaneNode pane)
        {
            if (pane == null) return;
            pane.IsAutoHide = !pane.IsAutoHide;
        }

        // 현재 메인/플로팅 레이아웃을 스냅샷으로 생성.
        public DockLayoutSnapshot CreateSnapshot()
        {
            var snapshot = new DockLayoutSnapshot { Root = ToDto(Root) };
            foreach (var fw in FloatingWindows)
            {
                snapshot.FloatingWindows.Add(new FloatingWindowSnapshot
                {
                    Left = fw.Left,
                    Top = fw.Top,
                    Width = fw.Width,
                    Height = fw.Height,
                    Root = ToDto(fw.Root)
                });
            }
            return snapshot;
        }

        // JSON 파일로 레이아웃 저장.
        public void SaveToFile(string filePath)
        {
            var json = JsonSerializer.Serialize(CreateSnapshot(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        // JSON 파일에서 레이아웃 복원.
        // resetFloating 콜백으로 기존 플로팅 창을 먼저 정리.
        public void LoadFromFile(string filePath, Action resetFloating)
        {
            if (!File.Exists(filePath)) return;

            var json = File.ReadAllText(filePath);
            var snapshot = JsonSerializer.Deserialize<DockLayoutSnapshot>(json);
            if (snapshot?.Root == null) return;

            resetFloating?.Invoke();
            Root = FromDto(snapshot.Root);

            if (snapshot.FloatingWindows == null) return;
            foreach (var f in snapshot.FloatingWindows)
            {
                var node = FromDto(f.Root);
                var pane = node as DockPaneNode;
                if (pane == null) continue;

                var window = new FloatingDockWindow(this, pane)
                {
                    Left = f.Left,
                    Top = f.Top,
                    Width = Math.Max(250, f.Width),
                    Height = Math.Max(200, f.Height)
                };

                FloatingWindows.Add(window);
                window.Show();
            }
        }

        // 트리 내부 oldNode를 newNode로 교체.
        public void ReplaceNode(DockNode oldNode, DockNode newNode)
        {
            if (Root == oldNode)
            {
                Root = newNode;
                return;
            }

            ReplaceNodeInternal(Root, oldNode, newNode);
        }

        // 탭이 없는 Pane를 제거하고, 의미 없는 Split을 접어 트리를 단순화.
        public void CleanupEmptyPanes()
        {
            Root = CleanupNode(Root);
            if (Root == null)
            {
                Root = new DockPaneNode();
            }
        }

        // 재귀적으로 빈 노드 정리.
        private DockNode CleanupNode(DockNode node)
        {
            if (node is DockPaneNode pane)
            {
                return pane.Items.Count == 0 ? null : pane;
            }

            var split = node as DockSplitNode;
            if (split == null) return node;

            split.First = CleanupNode(split.First);
            split.Second = CleanupNode(split.Second);

            if (split.First == null) return split.Second;
            if (split.Second == null) return split.First;
            return split;
        }

        // Root가 아닌 경우 재귀 탐색으로 노드 치환.
        private bool ReplaceNodeInternal(DockNode current, DockNode oldNode, DockNode newNode)
        {
            var split = current as DockSplitNode;
            if (split == null) return false;

            if (split.First == oldNode)
            {
                split.First = newNode;
                return true;
            }

            if (split.Second == oldNode)
            {
                split.Second = newNode;
                return true;
            }

            return ReplaceNodeInternal(split.First, oldNode, newNode) || ReplaceNodeInternal(split.Second, oldNode, newNode);
        }

        // 현재 트리의 모든 Pane를 순회.
        private IEnumerable<DockPaneNode> EnumeratePanes(DockNode node)
        {
            if (node == null) yield break;

            if (node is DockPaneNode pane)
            {
                yield return pane;
                yield break;
            }

            var split = node as DockSplitNode;
            if (split == null) yield break;

            foreach (var x in EnumeratePanes(split.First)) yield return x;
            foreach (var x in EnumeratePanes(split.Second)) yield return x;
        }

        // 런타임 노드 -> 저장 DTO 변환.
        private DockNodeDto ToDto(DockNode node)
        {
            if (node is DockPaneNode pane)
            {
                return new DockNodeDto
                {
                    NodeType = "Pane",
                    Id = pane.Id,
                    ActiveItemId = pane.ActiveItemId,
                    IsAutoHide = pane.IsAutoHide,
                    Items = pane.Items.Select(x => new DockItemDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        ContentId = x.ContentId,
                        IsFloating = x.IsFloating
                    }).ToList()
                };
            }

            var split = node as DockSplitNode;
            return new DockNodeDto
            {
                NodeType = "Split",
                Id = split.Id,
                Orientation = split.Orientation.ToString(),
                Ratio = split.Ratio,
                First = ToDto(split.First),
                Second = ToDto(split.Second)
            };
        }

        // 저장 DTO -> 런타임 노드 변환.
        private DockNode FromDto(DockNodeDto dto)
        {
            if (dto.NodeType == "Pane")
            {
                var pane = new DockPaneNode
                {
                    Id = dto.Id,
                    ActiveItemId = dto.ActiveItemId,
                    IsAutoHide = dto.IsAutoHide
                };

                if (dto.Items != null)
                {
                    foreach (var item in dto.Items)
                    {
                        pane.Items.Add(new DockItemModel
                        {
                            Id = item.Id,
                            Title = item.Title,
                            ContentId = item.ContentId,
                            IsFloating = item.IsFloating
                        });
                    }
                }

                pane.EnsureActive();
                return pane;
            }

            return new DockSplitNode
            {
                Id = dto.Id,
                Orientation = Enum.TryParse<DockSplitOrientation>(dto.Orientation, out var orientation)
                    ? orientation
                    : DockSplitOrientation.Horizontal,
                Ratio = dto.Ratio <= 0 || dto.Ratio >= 1 ? 0.5 : dto.Ratio,
                First = FromDto(dto.First),
                Second = FromDto(dto.Second)
            };
        }
    }
}
