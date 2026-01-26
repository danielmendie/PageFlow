using Microsoft.AspNetCore.Components;

namespace PageFlow.Blazor
{
    public interface IPageFlowNavigator
    {
        Task NavigateToAsync<TComponent>(object? parameters)
            where TComponent : IComponent;
        Task NavigateToAsync(Type componentType, object? parameters = null);
    }
}
