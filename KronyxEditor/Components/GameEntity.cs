using KronyxEditor.GameProject;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace KronyxEditor.Components
{
    [DataContract]
    [KnownType(typeof(Transform))] // Serializer에게 Transform이 GameEntity의 Component로 사용될 수 있음을 알려줌
    public class GameEntity : ViewModelBase
    {
        private string _name;
        private readonly ObservableCollection<Component> _components = new ObservableCollection<Component> ();

        public GameEntity(Scene parentScene)
        {
            Debug.Assert(parentScene != null);
            ParentScene = parentScene;
            _components.Add(new Transform(this));
            OnDeserialized(new StreamingContext()); // Components 컬렉션 초기화

        }

        [DataMember(Name = nameof(Components))]
        public ReadOnlyObservableCollection<Component> Components { get; private set; }

        [DataMember]
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [DataMember]
        public Scene ParentScene { get; set; }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            if(_components != null)
            {
                Components = new ReadOnlyObservableCollection<Component>(_components);
                OnPropertyChanged(nameof(Components));
            }
        }



    }
}
