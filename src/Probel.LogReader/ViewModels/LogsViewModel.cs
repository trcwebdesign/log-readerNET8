﻿using Caliburn.Micro;
using Probel.LogReader.Core.Configuration;
using Probel.LogReader.Core.Constants;
using Probel.LogReader.Core.Filters;
using Probel.LogReader.Core.Helpers;
using Probel.LogReader.Core.Plugins;
using Probel.LogReader.Helpers;
using Probel.LogReader.Models;
using Probel.LogReader.Ui;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Probel.LogReader.ViewModels
{
    public class LogsViewModel : Screen, IHandle<UiEvent>
    {
        #region Fields

        private static readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly IConfigurationManager _configManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _log;
        private readonly IUserInteraction _ui;
        private IEnumerable<LogRow> _cachedLogs;
        private bool _canListen;
        private int _changeCount;
        private DateTime _date;
        private ObservableCollection<IHierarchy<DateTime>> _days;
        private string _filePath;
        private string _filterApplied;
        private ObservableCollection<MenuItemModel> _filters;
        private string _gridLinesVisibility;
        private bool _hasLogs;
        private bool _isDebugVisible = true;
        private bool _isEmpty;
        private bool _isErrorVisible = true;
        private bool _isFatalVisible = true;
        private bool _isFile;
        private bool _isInfoVisible = true;
        private bool _isListeningFile;
        private bool _isLoggerVisible = true;
        private bool _isOrderByAsc;
        private bool _isThreadIdVisible;
        private bool _isTraceVisible = true;
        private bool _isWarnVisible = true;
        private DateTime _lastRefresh = DateTime.Now;
        private ObservableCollection<LogRow> _logs;

        private ObservableCollection<MenuItemModel> _repositories;
        private string _repositoryName;

        private bool _showPersistenceButtons;

        #endregion Fields

        #region Constructors

        public LogsViewModel(
            IConfigurationManager configManager,
            IEventAggregator eventAggregator,
            ILogger log,
            IUserInteraction ui)
        {
            eventAggregator.Subscribe(this);

            DockingStateFile = Environment.ExpandEnvironmentVariables(configManager?.Get()?.Ui?.LayoutFile ?? string.Empty);

            _ui = ui;
            _log = log;
            FilterCommand = new RelayCommand(Filter);
            ResetFilterCommand = new RelayCommand(ResetFilter);

            _eventAggregator = eventAggregator;
            _configManager = configManager;
            _stopwatch.Start();
        }

        #endregion Constructors

        #region Properties

        public bool CanListen
        {
            get => _canListen;
            set => Set(ref _canListen, value, nameof(CanListen));
        }

        public int ChangeCount
        {
            get => _changeCount;
            set => Set(ref _changeCount, value, nameof(ChangeCount));
        }

        public DateTime Date
        {
            get => _date;
            set => Set(ref _date, value, nameof(Date));
        }

        public ObservableCollection<IHierarchy<DateTime>> Days
        {
            get => _days;
            private set
            {
                Set(ref _days, value, nameof(Days));
                IsEmpty = _days.Count() == 0;
            }
        }

        public string DockingStateFile { get; private set; }

        public string FilePath
        {
            get => _filePath;
            set => Set(ref _filePath, value, nameof(FilePath));
        }

        public string FilterApplied
        {
            get => _filterApplied;
            set => Set(ref _filterApplied, value, nameof(FilterApplied));
        }

        public ICommand FilterCommand { get; set; }

        public ObservableCollection<MenuItemModel> Filters
        {
            get => _filters;
            set => Set(ref _filters, value);
        }

        public string GridLinesVisibility
        {
            get => _gridLinesVisibility;
            set => Set(ref _gridLinesVisibility, value, nameof(GridLinesVisibility));
        }

        public bool HasLogs
        {
            get => _hasLogs;
            set => Set(ref _hasLogs, value);
        }

        public bool IsDebugVisible
        {
            get => _isDebugVisible;
            set => Set(ref _isDebugVisible, value, nameof(IsDebugVisible));
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set => Set(ref _isEmpty, value);
        }

        public bool IsErrorVisible
        {
            get => _isErrorVisible;
            set => Set(ref _isErrorVisible, value, nameof(IsErrorVisible));
        }

        public bool IsFatalVisible
        {
            get => _isFatalVisible;
            set => Set(ref _isFatalVisible, value, nameof(IsFatalVisible));
        }

        public bool IsFile
        {
            get => _isFile;
            set => Set(ref _isFile, value, nameof(IsFile));
        }

        public bool IsInfoVisible
        {
            get => _isInfoVisible;
            set => Set(ref _isInfoVisible, value, nameof(IsInfoVisible));
        }

        public bool IsListeningFile
        {
            get => _isListeningFile;
            set
            {
                if (Set(ref _isListeningFile, value, nameof(IsListeningFile)))
                {
                    if (value) { RegisterListener(); }
                    else { UnregisterListener(); }
                }
            }
        }

        public bool IsLoggerVisible
        {
            get => _isLoggerVisible;
            set => Set(ref _isLoggerVisible, value, nameof(IsLoggerVisible));
        }

        public bool IsOrderByAsc
        {
            get => _isOrderByAsc;
            set => Set(ref _isOrderByAsc, value, nameof(IsOrderByAsc));
        }

        public bool IsThreadIdVisible
        {
            get => _isThreadIdVisible;
            set => Set(ref _isThreadIdVisible, value, nameof(IsThreadIdVisible));
        }

        public bool IsTraceVisible
        {
            get => _isTraceVisible;
            set => Set(ref _isTraceVisible, value, nameof(IsTraceVisible));
        }

        public bool IsWarnVisible
        {
            get => _isWarnVisible;
            set => Set(ref _isWarnVisible, value, nameof(IsWarnVisible));
        }

        public IFilter LastFilter { get; internal set; }

        public DateTime LastRefresh
        {
            get => _lastRefresh;
            set => Set(ref _lastRefresh, value, nameof(LastRefresh));
        }

        public IDataListener Listener { get; set; }

        public ObservableCollection<LogRow> Logs
        {
            get => _logs;
            set
            {
                if (Set(ref _logs, value, nameof(Logs)))
                {
                    NotifyOfPropertyChange(nameof(LogsCount));
                    HasLogs = value.Count > 0;
                }
            }
        }

        public int LogsCount => Logs?.Count() ?? 0;

        public IPlugin Plugin { get; internal set; }

        public ObservableCollection<MenuItemModel> Repositories
        {
            get => _repositories;
            set => Set(ref _repositories, value);
        }

        public string RepositoryName
        {
            get => _repositoryName;
            set => Set(ref _repositoryName, value, nameof(RepositoryName));
        }

        public ICommand ResetFilterCommand { get; set; }

        public bool ShowPersistenceButtons
        {
            get => _showPersistenceButtons;
            set => Set(ref _showPersistenceButtons, value);
        }

        #endregion Properties

        #region Methods

        private IEnumerable<LogRow> Filter(IEnumerable<LogRow> logs)
        {
            var src = logs ?? _cachedLogs;
            var levels = GetLevels();

            var filtered = (from l in src ?? new List<LogRow>()
                            where levels.Contains(l.Level.ToLower())
                            select l).ToList();

            return IsOrderByAsc
                ? filtered.OrderBy(e => e.Time).ToList()
                : filtered.OrderByDescending(e => e.Time).ToList();
        }

        private IEnumerable<string> GetLevels()
        {
            var levels = new List<string>();
            if (IsTraceVisible) { levels.Add("trace"); }
            if (IsDebugVisible) { levels.Add("debug"); }
            if (IsInfoVisible) { levels.Add("info"); }
            if (IsWarnVisible) { levels.Add("warn"); }
            if (IsErrorVisible) { levels.Add("error"); }
            if (IsFatalVisible) { levels.Add("fatal"); }
            return levels;
        }

        private void OnDataChanged(object sender, EventArgs e)
        {
            if (IsListeningFile)
            {
                _stopwatch.Stop();
                if (_stopwatch.ElapsedMilliseconds > 500)
                {
                    _log.Trace($"Log file changed! Last event {_stopwatch.ElapsedMilliseconds} msec ago.");
                    ChangeCount++;
                    Task.Run(() =>
                    {
                        RefreshData();
                    }).OnErrorHandle(_ui);
                }
            }
            _stopwatch.Reset();
            _stopwatch.Start();
        }

        private void RegisterListener()
        {
            if (Listener != null)
            {
                _log.Trace($"Activate log change listener.");
                Listener.DataChanged += OnDataChanged;
                Listener.StartListening(Date);
            }
            else { _log.Warn("Plugin can listen but no listener is configured!"); }
        }

        private void ResetFilter()
        {
            Logs = new ObservableCollection<LogRow>(_cachedLogs);
        }

        private void SaveConfig() => _configManager.Save(cfg =>
        {
            cfg.Ui.IsLogOrderAsc = IsOrderByAsc;
            cfg.Ui.IsLoggerVisible = IsLoggerVisible;
            cfg.Ui.IsThreadIdVisible = IsThreadIdVisible;
        });

        private void SortLogs(bool sortAsc)
        {
            var l = (sortAsc)
                ? Logs.OrderBy(e => e.Time)
                : Logs.OrderByDescending(e => e.Time);
            Logs = new ObservableCollection<LogRow>(l);
        }

        private void UnregisterListener()
        {
            if (Listener != null)
            {
                _log.Trace($"DEACTIVATE log change listener.");
                Listener.DataChanged -= OnDataChanged;
            }
            else { _log.Trace("No listener to deactivate."); }

            ChangeCount = 0;
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            _eventAggregator.PublishOnUIThreadAsync(UiEvent.ShowMenuFilter());
            IsTraceVisible
                = IsDebugVisible
                = IsInfoVisible
                = IsWarnVisible
                = IsErrorVisible
                = IsFatalVisible
                = true;
            if (IsListeningFile) { RegisterListener(); }

            GridLinesVisibility = _configManager.Get()?.Ui?.GridLineVisibility?.ToString() ?? "All";
            ShowPersistenceButtons = _configManager.Get().Ui.ShowLayoutButtons;

            IsEmpty = (Days?.Count ?? 0) == 0;
            return Task.CompletedTask;
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            _eventAggregator.PublishOnUIThreadAsync(UiEvent.HideMenuFilter());
            UnregisterListener();

            var t1 = Task.Run(() => SaveConfig());
            t1.OnErrorHandle(_ui);
            return Task.CompletedTask;
        }

        public void Cache(IEnumerable<LogRow> logs) => _cachedLogs = logs;

        public void ExecuteFilter(MenuItemModel filter)
        {
            if (filter == null) { return; }
            else if (filter.MenuCommand.CanExecute(null))
            {
                filter.MenuCommand.Execute(null);
            }
        }

        /// <summary>
        /// This method is used for the <see cref="ICommand"/>
        /// </summary>
        public void Filter()
        {
            var logs = Filter(null);
            Logs = new ObservableCollection<LogRow>(logs);
        }

        public void FilterMessage(string criterion)
        {
            if (string.IsNullOrEmpty(criterion)) { Logs = new ObservableCollection<LogRow>(_cachedLogs); }
            else
            {
                criterion = criterion.ToLower();
                var logs = from l in Logs
                           where l.Message.ToLower().Contains(criterion)
                           select l;
                Logs = new ObservableCollection<LogRow>(logs);
            }
        }

        public IEnumerable<LogRow> GetLogRows() => Plugin.GetLogs(Date, _isOrderByAsc ? OrderBy.Asc : OrderBy.Desc);

        public void Handle(UiEvent message)
        {
            if (message.Event == UiEvents.FilterApplied)
            {
                if (message.Context is string msg) { FilterApplied = msg; }
                else if (message.Context == null) { FilterApplied = null; }
            }
        }
        public async Task HandleAsync(UiEvent message, CancellationToken cancellationToken) {
            if (message.Event == UiEvents.FilterApplied) {
                if (message.Context is string msg) { FilterApplied = msg; } else if (message.Context == null) { FilterApplied = null; }
            }
        }

        public void LoadDays(IEnumerable<DateTime> days)
        {
            Days = new ObservableCollection<IHierarchy<DateTime>>(days.ToHierarchy());

            //Expand first year, first month; collapse all other
            if (Days.Count() > 0)
            {
                Days[0].IsExpanded = true;
                if (Days[0].Children.Count() > 0)
                {
                    var item = Days[0].Children[0];
                    item.IsExpanded = true;
                }
            }
        }

        public void LoadLogs(DateTime day)
        {
            var token = new CancellationToken();
            var scheduler = TaskScheduler.Current;

            var t1 = Task.Run(() =>
            {
                using (_ui.NotifyWait())
                {
                    Date = day;
                    var logs = Plugin.GetLogs(day);
                    Cache(logs);
                    var l = Filter(logs);
                    return l;
                }
            });
            t1.OnErrorHandle(_ui);

            var t2 = t1.ContinueWith(r =>
            {
                _eventAggregator.PublishOnUIThreadAsync(UiEvent.FilterApplied(string.Empty));
                Logs = new ObservableCollection<LogRow>(r.Result);
            });
            t2.OnErrorHandle(_ui, token, scheduler);
        }

        private MenuItemModel _loadedRepository;
        private void ReloadRepository()
        {
            if (_loadedRepository != null)
            {
                /* Configuration is saved only when LogView
                 * is deactivated. Whenever you reloads the
                 * repositories, settings is reladed from files
                 * Therefore, this is why I save it here
                 */
                SaveConfig();

                LoadRepository(_loadedRepository);
            }
        }
        public void LoadRepository(MenuItemModel repository)
        {
            if (repository == null) { return; }
            else if (repository.MenuCommand.CanExecute(null))
            {
                _loadedRepository = repository;
                repository.MenuCommand.Execute(null);
            }
        }

        public void RefreshData()
        {
            var l = GetLogRows();
            Cache(l);
            var logs = Filter(LastFilter?.Filter(l) ?? l);
            Logs = new ObservableCollection<LogRow>(logs);
            LastRefresh = DateTime.Now;

            ReloadRepository();
        }

        /// <summary>
        /// Refreshes the logs
        /// </summary>
        /// <param name="notifyUser">Indicates whether to notify user logs has been updated</param>
        public void RefreshLogs(bool notifyUser = true)
        {
            if (notifyUser) { _ui.NotifyInformation("Refreshing logs..."); }
            var t1 = new Task(() => RefreshData());
            t1.OnErrorHandle(_ui);

            t1.ContinueWith(r =>
            {
                if (notifyUser) { _ui.NotifySuccess("Refresh done."); }
                LastRefresh = DateTime.Now;
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            t1.Start();
        }

        public void RefreshLogs() => RefreshLogs(true);

        public void ResetFromCache()
        {
            if (_cachedLogs == null) { Logs = new ObservableCollection<LogRow>(); }
            else
            {
                var l = IsOrderByAsc
                    ? _cachedLogs.OrderBy(e => e.Time)
                    : _cachedLogs.OrderByDescending(e => e.Time);
                Logs = new ObservableCollection<LogRow>(l);
            }
        }

        public void ToggleSortLogs()
        {
            IsOrderByAsc = !IsOrderByAsc;
            SortLogs(IsOrderByAsc);
            SaveConfig();
        }

        #endregion Methods
    }
}