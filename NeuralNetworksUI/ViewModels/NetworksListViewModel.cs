using System;
using NeuralNetworksUI.Views;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;

namespace NeuralNetworksUI.ViewModels
{
    public class NetworksListViewModel : BindableBase
    {
        private readonly IContainerExtension _container;
        private readonly IRegionManager _regionManager;

        public NetworksListViewModel(IRegionManager regionManager, IContainerExtension container)
        {
            _regionManager = regionManager;
            _container = container;

            SelectNeuralNetworkCommand = new DelegateCommand<string>(ExecuteSelectNeuralNetworkCommand);
        }

        public DelegateCommand<string> SelectNeuralNetworkCommand { get; set; }

        private void ExecuteSelectNeuralNetworkCommand(string neuralNetwork)
        {
            switch (neuralNetwork)
            {
                case "Adaline":
                    _regionManager.RequestNavigate("NeuralNetwork", typeof(Views.Adaline).Name);
                    break;

                case "KohonenCards":
                    _regionManager.RequestNavigate("NeuralNetwork", typeof(KohonenCards).Name);
                    break;

                default:
                    throw new Exception("Neural network is not implemented.");
            }
        }
    }
}