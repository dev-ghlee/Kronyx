using KronyxEditor.Components;
using KronyxEditor.GameProject;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KronyxEditor.Editors
{
    /// <summary>
    /// ProjectLayoutView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ProjectLayoutView : UserControl
    {
        public ProjectLayoutView()
        {
            InitializeComponent();
        }

        private void OnAddGameEntity_Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var vm = btn.DataContext as Scene; // parent scene
            vm.AddGameEntityCommand.Execute(new GameEntity(vm) { Name = "Empty Game Entity"});
        }

        private void OnGameEntities_ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GameEntityView.Instance.DataContext = null; // 선택된 엔티티가 변경될 때마다 DataContext를 먼저 null로 설정하여 이전 엔티티와의 바인딩을 끊음
            if (e.AddedItems.Count > 0)
            {
                GameEntityView.Instance.DataContext = (sender as ListBox).SelectedItems[0];
             
            }
        }
    }
}
