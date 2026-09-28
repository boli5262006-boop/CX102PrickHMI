using CommunityToolkit.Mvvm.ComponentModel;

namespace CX102PrickHMI.Models
{
    public sealed class ProcessStep : ObservableObject
    {
        private string _status;
        private ProcessStepState _state;

        public ProcessStep(string title, string status, ProcessStepState state, int number)
        {
            Title = title;
            _status = status;
            _state = state;
            Number = number;
        }

        public string Title { get; }
        public string Status
        {
            get { return _status; }
            private set { SetProperty(ref _status, value); }
        }

        public ProcessStepState State
        {
            get { return _state; }
            private set { SetProperty(ref _state, value); }
        }

        public int Number { get; }

        public void UpdateState(ProcessStepState state, string status)
        {
            State = state;
            Status = status;
        }
    }

    public enum ProcessStepState
    {
        Pending,
        Completed,
        Error
    }
}
