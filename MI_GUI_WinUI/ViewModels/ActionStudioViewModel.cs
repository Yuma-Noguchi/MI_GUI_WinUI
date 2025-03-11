using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using System.Linq;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ActionStudioViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = "Action Studio";

        [ObservableProperty]
        private ObservableCollection<Models.Action> _actions;

        [ObservableProperty]
        private Models.Action _selectedAction;

        [ObservableProperty]
        private string _selectedButton;

        [ObservableProperty]
        private double _sleepDuration = 1.0;

        public bool IsActionSelected => SelectedAction != null;

        public ObservableCollection<string> AvailableButtons { get; } = new()
        {
            "A", "B", "X", "Y", "LB", "RB", "LT", "RT",
            "Start", "Back", "LS", "RS",
            "DPad_Up", "DPad_Down", "DPad_Left", "DPad_Right"
        };

        public ObservableCollection<Dictionary<string, string[]>> ActionSequence { get; } = new();

        private readonly string ACTIONS_DIR;

        public ActionStudioViewModel()
        {
            _actions = new ObservableCollection<Models.Action>();
            ACTIONS_DIR = Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                "MotionInput", "data", "assets", "generated_actions"
            );
        }

        [RelayCommand]
        private void CreateAction()
        {
            var newAction = new Models.Action
            {
                Name = $"New Action {_actions.Count + 1}",
                Description = "Enter action description..."
            };
            
            Actions.Add(newAction);
            SelectedAction = newAction;
            ActionSequence.Clear();
            SleepDuration = 1.0;
        }

        [RelayCommand]
        private void AddToSequence()
        {
            if (string.IsNullOrEmpty(SelectedButton)) return;

            ActionSequence.Add(new Dictionary<string, string[]>
            {
                { "press", new[] { SelectedButton.ToLower() } }
            });
        }

        [RelayCommand]
        private void RemoveFromSequence(int index)
        {
            if (index >= 0 && index < ActionSequence.Count)
            {
                ActionSequence.RemoveAt(index);
            }
        }

        [RelayCommand]
        private async Task SaveSequenceAsync()
        {
            if (SelectedAction == null || !ActionSequence.Any()) return;

            try
            {
                if (!Directory.Exists(ACTIONS_DIR))
                {
                    Directory.CreateDirectory(ACTIONS_DIR);
                }

                var actionData = new
                {
                    action = new
                    {
                        @class = "ds4_gamepad",
                        method = "chain",
                        args = ActionSequence.Concat(new object[] { SleepDuration }).ToList()
                    }
                };

                var json = JsonConvert.SerializeObject(actionData, Formatting.Indented);
                var filePath = Path.Combine(ACTIONS_DIR, $"{SelectedAction.Name}.json");
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                // TODO: Error handling
                System.Diagnostics.Debug.WriteLine($"Error saving action: {ex.Message}");
            }
        }

        [RelayCommand]
        private void DeleteAction()
        {
            if (SelectedAction == null) return;

            Actions.Remove(SelectedAction);
            SelectedAction = Actions.FirstOrDefault();
        }

        partial void OnSelectedActionChanged(Models.Action value)
        {
            OnPropertyChanged(nameof(IsActionSelected));
        }
    }
}
