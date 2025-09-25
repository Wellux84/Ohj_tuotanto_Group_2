using CommunityToolkit.Mvvm.Input;
using Group_2.Models;

namespace Group_2.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}