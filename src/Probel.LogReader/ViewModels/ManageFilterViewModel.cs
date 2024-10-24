﻿using Caliburn.Micro;
using Probel.LogReader.Core.Configuration;
using Probel.LogReader.Core.Helpers;
using Probel.LogReader.Core.Plugins;
using Probel.LogReader.Helpers;
using Probel.LogReader.Properties;
using Probel.LogReader.Ui;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Probel.LogReader.ViewModels
{
    public class ManageFilterViewModel : Conductor<IScreen>
    {
        #region Fields

        private readonly IConfigurationManager _configManager;
        private readonly EditFilterViewModel _editFilterViewModel;
        private readonly IEventAggregator _eventAggregator;
        private readonly IUserInteraction _userInteraction;
        private AppSettings _app;
        private FilterSettings _currentFilterSettings;
        private ObservableCollection<FilterSettings> _filters;

        public readonly ILogger _log;

        #endregion Fields

        #region Constructors
        public ICommand CreateFilterCommand { get; private set; }
        public ICommand CreateSubFilterCommand { get; private set; }
        public ManageFilterViewModel(IConfigurationManager configManager
            , EditFilterViewModel editSubfilterViewModel
            , IEventAggregator eventAggregator
            , IUserInteraction userInteraction
            , ILogger log)
        {
            DeleteCurrentFilterCommand = new RelayCommand(DeleteCurrentFilter);
            SaveAllCommand = new RelayCommand(SaveAll);
            CreateSubFilterCommand = new RelayCommand(CreateSubFilter);
            CreateFilterCommand = new RelayCommand(CreateFilter);

            _log = log;
            _userInteraction = userInteraction;
            _eventAggregator = eventAggregator;
            _editFilterViewModel = editSubfilterViewModel;
            _configManager = configManager;
        }

        #endregion Constructors

        #region Properties

        public bool CanCreateSubFilter => _editFilterViewModel.Subfilters != null;

        public FilterSettings CurrentFilter
        {
            get => _currentFilterSettings;
            set => Set(ref _currentFilterSettings, value, nameof(CurrentFilter));
        }

        public ICommand DeleteCurrentFilterCommand { get; private set; }

        public ObservableCollection<FilterSettings> Filters
        {
            get => _filters;
            set => Set(ref _filters, value, nameof(Filters));
        }

        public ICommand SaveAllCommand { get; private set; }

        #endregion Properties

        #region Methods

        private void DeleteCurrentFilter()
        {
            var toDel = (from f in Filters
                         where f.Id == CurrentFilter.Id
                         select f).FirstOrDefault();

            if (_userInteraction.Ask(Strings.Msg_AskDelete) == UserAnswers.Yes)
            {
                if (toDel != null)
                {
                    Filters.Remove(toDel);
                    _configManager.Decorate(_app).Remove(toDel);
                }
            }
        }

        public void ActivateCurrentFilter()
        {
            DeactivateItemAsync(_editFilterViewModel, true);

            if (CurrentFilter != null)
            {
                _editFilterViewModel.SetSubfilters(CurrentFilter);
                NotifyOfPropertyChange(() => CanCreateSubFilter);
                ActivateItemAsync(_editFilterViewModel);
            }
        }

        public void CreateFilter()
        {
            var newFilter = new FilterSettings("new empty");
            Filters.Add(newFilter);
            _app.Filters.Add(newFilter);
            CurrentFilter = newFilter;
        }

        public void CreateSubFilter() => _editFilterViewModel?.CreateSubfilter();

        public void DiscardAll()
        {
            if (_userInteraction.Ask(Strings.Msg_AskReset) == UserAnswers.Yes)
            {
                Load();
            }
        }

        public void Load()
        {
            var t1 = Task.Run(() =>
            {
                _app = _configManager.Get();

                var filters = (from f in _app.Filters
                               where f.Id != FilterSettings.NoFilter.Id
                               select f).ToList();

                Filters = new ObservableCollection<FilterSettings>(filters);
            });
            t1.OnErrorHandle(_userInteraction);
        }

        public void SaveAll()
        {
            var t1 = Task.Run(() =>
            {
                _configManager.SaveAsync(_app);

                _eventAggregator.PublishOnBackgroundThreadAsync(UiEvent.RefreshMenus);
                _userInteraction.NotifySuccess(Strings.Msg_InformFilterSaved);
            });
            t1.OnErrorHandle(_userInteraction);
        }

        #endregion Methods
    }
}