﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using OFT.Attributes;
using ATAS.Indicators.Properties;

namespace ATAS.Indicators.Technical
{
    [Category("Bid x Ask,Delta,Volume")]
    [HelpLink("https://support.orderflowtrading.ru/knowledge-bases/2/articles/2471-volume")]
    public class Volume2 : Indicator
    {
        #region Nested types

        public enum InputType
        {
            Volume = 0,
            Ticks = 1
        }

        #endregion

        #region Fields

        private readonly ValueDataSeries _filterseries;
        private readonly ValueDataSeries _negative;
        private readonly ValueDataSeries _neutral;
        private readonly ValueDataSeries _positive;

        private readonly ValueDataSeries _topLevel;
        private readonly ValueDataSeries _lowerLevel;

        private bool _alerted;
        private bool _deltaColored;
        private decimal _filter;
        private InputType _input = InputType.Volume;
        private int _lastBar;
        private bool _useFilter;

        private int _topLevelValue;
        private int _lowerLevelValue;
        private Color _topLevelColor;
        private Color _lowerLevelColor;

        #endregion

        #region Properties

        [Display(ResourceType = typeof(Resources), Name = "DeltaColored", GroupName = "Colors")]
        public bool DeltaColored
        {
            get => _deltaColored;
            set
            {
                _deltaColored = value;
                RaisePropertyChanged("DeltaColored");
                RecalculateValues();
            }
        }

        [Display(ResourceType = typeof(Resources), Name = "UseTimeFilter", GroupName = "Filter")]
        public bool UseFilter
        {
            get => _useFilter;
            set
            {
                _useFilter = value;
                RaisePropertyChanged("UseFilter");
                RecalculateValues();
            }
        }

        [Display(ResourceType = typeof(Resources), Name = "Filter", GroupName = "Filter")]
        public decimal FilterValue
        {
            get => _filter;
            set
            {
                _filter = value;
                RaisePropertyChanged("Filter");
                RecalculateValues();
            }
        }

        [Display(ResourceType = typeof(Resources), Name = "Type", GroupName = "Calculation")]
        public InputType Input
        {
            get => _input;
            set
            {
                _input = value;
                RaisePropertyChanged("Type");
                RecalculateValues();
            }
        }

        [Display(ResourceType = typeof(Resources), Name = "UseAlerts", GroupName = "Alerts")]
        public bool UseAlerts { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "AlertFile", GroupName = "Alerts")]
        public string AlertFile { get; set; } = "alert1";

        [Display(Name = "Value", GroupName = " Top percentage level")]
        public int TopLevelValue
        {
            get => _topLevelValue;
            set
            {
                _topLevelValue = ValueValidation(value);
                RaisePropertyChanged("Top");
                RecalculateValues();
            }
        }

        [Display(Name = "Color", GroupName = " Top percentage level")]
        public Color TopLevelColor
        {
            get => _topLevelColor;
            set
            {
                _topLevelColor = value;
                RaisePropertyChanged("Top");
                RecalculateValues();
            }
        }

        [Display(Name = "Value", GroupName = "Low percentage level")]
        public int LowerLevelValue
        {
            get => _lowerLevelValue;
            set
            {
                _lowerLevelValue = ValueValidation(value);
                RaisePropertyChanged("Low");
                RecalculateValues();
            }
        }

        [Display(Name = "Color", GroupName = "Low percentage level")]
        public Color LowerLevelColor
        {
            get => _lowerLevelColor;
            set
            {
                _lowerLevelColor = value;
                RaisePropertyChanged("Low");
                RecalculateValues();
            }
        }

        #endregion

        #region ctor

        public Volume2() : base(true)
        {
            Panel = IndicatorDataProvider.NewPanel;
            _positive = (ValueDataSeries)DataSeries[0];
            _positive.Color = Colors.Green;
            _positive.VisualType = VisualMode.Histogram;
            _positive.ShowZeroValue = false;
            _positive.Name = "Positive";

            _lastBar = -1;

            _negative = new ValueDataSeries("Negative")
            {
                Color = Colors.Red,
                VisualType = VisualMode.Histogram,
                ShowZeroValue = false
            };
            DataSeries.Add(_negative);

            _neutral = new ValueDataSeries("Neutral")
            {
                Color = Colors.Gray,
                VisualType = VisualMode.Histogram,
                ShowZeroValue = false
            };
            DataSeries.Add(_neutral);

            _filterseries = new ValueDataSeries("Filter")
            {
                Color = Colors.LightBlue,
                VisualType = VisualMode.Histogram,
                ShowZeroValue = false
            };
            DataSeries.Add(_filterseries);

            _topLevel = new ValueDataSeries("Top")
            {
                Color = _topLevelColor,
                VisualType = VisualMode.Line,
                ShowCurrentValue = false
            };
            DataSeries.Add(_topLevel);

            _lowerLevel = new ValueDataSeries("Low")
            {
                Color = _lowerLevelColor,
                VisualType = VisualMode.Line,
                ShowCurrentValue = false
            };
            DataSeries.Add(_lowerLevel);
        }

        #endregion

        #region Public methods

        public override string ToString()
        {
            return "Volume 2";
        }

        #endregion

        #region Protected methods

        protected override void OnCalculate(int bar, decimal value)
        {
            if (_lastBar != bar)
                _alerted = false;

            var candle = GetCandle(bar);
            var val = candle.Volume;

            SetLines(candle.Volume, bar);

            if (UseAlerts && bar == CurrentBar - 1 && !_alerted && val >= _filter && _filter != 0)
            {
                AddAlert(AlertFile, $"Candle volume: {val}");
                _alerted = true;
            }

            _lastBar = bar;

            if (Input == InputType.Ticks)
                val = candle.Ticks;

            if (_useFilter && val > _filter)
            {
                _filterseries[bar] = val;
                _positive[bar] = _negative[bar] = _neutral[bar] = 0;
                return;
            }

            _filterseries[bar] = 0;

            if (_deltaColored)
            {
                if (candle.Delta > 0)
                {
                    _positive[bar] = val;
                    _negative[bar] = _neutral[bar] = 0;
                }
                else if (candle.Delta < 0)
                {
                    _negative[bar] = val;
                    _positive[bar] = _neutral[bar] = 0;
                }
                else
                {
                    _neutral[bar] = val;
                    _positive[bar] = _negative[bar] = 0;
                }
            }
            else
            {
                if (candle.Close > candle.Open)
                {
                    _positive[bar] = val;
                    _negative[bar] = _neutral[bar] = 0;
                }
                else if (candle.Close < candle.Open)
                {
                    _negative[bar] = val;
                    _positive[bar] = _neutral[bar] = 0;
                }
                else
                {
                    _neutral[bar] = val;
                    _positive[bar] = _negative[bar] = 0;
                }
            }
        }

        #endregion

        private void SetLines(decimal volume, int bar)
        {
            _topLevel[bar] = volume / 100 * TopLevelValue;
            _topLevel.Color = TopLevelColor;
            _topLevel.Name = $"{TopLevelValue}%";

            _lowerLevel[bar] = volume / 100 * LowerLevelValue;
            _lowerLevel.Color = LowerLevelColor;
            _lowerLevel.Name = $"{LowerLevelValue}%";
        }

        private int ValueValidation(int value)
        {
            if (value > 100)
            {
                value = 100;
            }

            if (value < 0)
            {
                value = 0;
            }

            return value;
        }
    }
}
