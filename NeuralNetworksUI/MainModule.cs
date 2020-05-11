using NeuralNetworksUI.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Prism.Unity;

namespace NeuralNetworksUI
{
    public class MainModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public MainModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.GetContainer().RegisterTypeForNavigation<NetworksList>();
            containerRegistry.GetContainer().RegisterTypeForNavigation<Views.Adaline>();
            containerRegistry.GetContainer().RegisterTypeForNavigation<KohonenCards>();
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            _regionManager.RequestNavigate("NeuralNetwork", typeof(NetworksList).Name);
        }
    }
}