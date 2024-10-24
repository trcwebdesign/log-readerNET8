﻿using Caliburn.Micro;
using Probel.LogReader.Colouration;
using Probel.LogReader.Core.Configuration;
using Probel.LogReader.Core.Helpers;
using Probel.LogReader.Core.Plugins;
using Probel.LogReader.Ui;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Probel.LogReader.ViewModels
{
    public class EditRepositoryViewModel : Screen
    {
        #region Fields

        public readonly IUserInteraction _user;
        private readonly IPluginInfoManager _infoManager;
        private IColourator _colourator;
        private ObservableCollection<PluginInfo> _pluginInfo;
        private RepositorySettings _repository;
        private PluginInfo _selectedPlugin;

        #endregion Fields

        #region Constructors

        public EditRepositoryViewModel(IPluginInfoManager infoManager, IUserInteraction userInteraction, ILogger logger)
        {
            _logger = logger;
            _user = userInteraction;
            _infoManager = infoManager;
        }

        #endregion Constructors

        #region Properties

        private readonly ILogger _logger;
        public bool CanDeleteRepository => Repository.HasValidId();

        public ObservableCollection<PluginInfo> PluginInfoList
        {
            get => _pluginInfo;
            set => Set(ref _pluginInfo, value, nameof(PluginInfoList));
        }

        public RepositorySettings Repository
        {
            get => _repository;
            set
            {
                if (Set(ref _repository, value, nameof(Repository)))
                {
                    NotifyOfPropertyChange(() => CanDeleteRepository);
                }
            }
        }

        public PluginInfo SelectedPlugin
        {
            get => _selectedPlugin;
            set
            {
                if (Set(ref _selectedPlugin, value, nameof(SelectedPlugin)))
                {
                    ActivateColourator(value?.Colouration);
                }
            }
        }

        #endregion Properties

        #region Methods

        public void Load()
        {
            PluginInfoList = new ObservableCollection<PluginInfo>(_infoManager.GetPluginsInfo());

            SelectedPlugin = (from p in PluginInfoList
                              where p.Id == (Repository?.PluginId ?? new Guid())
                              select p).FirstOrDefault();
        }

        public void Refresh(IColourator c)
        {
            _colourator = c;
            ActivateColourator(SelectedPlugin?.Colouration);
        }

        public void RefreshForUpdate()
        {
            if (Repository != null)
            {
                Repository.PluginId = SelectedPlugin?.Id ?? new Guid();
            }
            else { _logger.Debug("Current repository is null. Skip save."); }
        }

        protected async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken) => Repository = new RepositorySettings();

        private void ActivateColourator(string colouration) => _colourator?.Set(colouration);

        #endregion Methods
    }
}