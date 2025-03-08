using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using System.Linq;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ActionStudioViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = "Action Studio";

        [ObservableProperty]
        private ObservableCollection<Action> _actions;

        [ObservableProperty]
        private Action _selectedAction;

        public bool IsActionSelected => SelectedAction != null;

        public ActionStudioViewModel()
        {
            _actions = new ObservableCollection<Action>();
        }

        [RelayCommand]
        private void CreateAction()
        {
            var newAction = new Action
            {
                Name = $"New Action {_actions.Count + 1}",
                Description = "Enter action description..."
            };
            
            Actions.Add(newAction);
            SelectedAction = newAction;
        }

        [RelayCommand]
        private void DeleteAction()
        {
            if (SelectedAction == null) return;

            Actions.Remove(SelectedAction);
            SelectedAction = Actions.FirstOrDefault();
        }

        partial void OnSelectedActionChanged(Action value)
        {
            OnPropertyChanged(nameof(IsActionSelected));
        }
    }
}
