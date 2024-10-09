namespace StockSharp.Algo.Indicators
{
    [Display(
        ResourceType = typeof(LocalizedStrings),
        Name = nameof(LocalizedStrings.DMIKey),
        Description = nameof(LocalizedStrings.WellesWilderDirectionalMovementIndexKey))]
    [Doc("topics/api/indicators/list_of_indicators/dmi.html")]
    public class DirectionalIndex : BaseComplexIndicator
    {
        private class DxValue : ComplexIndicatorValue
        {
            private decimal _value;

            public DxValue(IComplexIndicator indicator, DateTimeOffset time)
                : base(indicator, time)
            {
            }

            public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
            {
                IsEmpty = false;
                _value = Convert.ToDecimal(value);
                return new DecimalIndicatorValue(indicator, _value, Time) { IsFinal = IsFinal };
            }

            public override T GetValue<T>(Level1Fields? field = null)
            {
                return (T)Convert.ChangeType(_value, typeof(T));
            }
        }

        public DirectionalIndex()
        {
            Plus = new DiPlus();
            Minus = new DiMinus();
            AddInner(Plus);
            AddInner(Minus);
        }

        public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

        private int _length;

        public int Length
        {
            get => _length;
            set
            {
                if (_length != value)
                {
                    _length = value;
                    Plus.Length = value;
                    Minus.Length = value;
                    Reset();
                }
            }
        }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Display(
            ResourceType = typeof(LocalizedStrings),
            Name = nameof(LocalizedStrings.DiPlusKey),
            Description = nameof(LocalizedStrings.DiPlusLineKey),
            GroupName = nameof(LocalizedStrings.GeneralKey))]
        public DiPlus Plus { get; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Display(
            ResourceType = typeof(LocalizedStrings),
            Name = nameof(LocalizedStrings.DiMinusKey),
            Description = nameof(LocalizedStrings.DiMinusLineKey),
            GroupName = nameof(LocalizedStrings.GeneralKey))]
        public DiMinus Minus { get; }

        protected override IIndicatorValue OnProcess(IIndicatorValue input)
        {
            var value = new DxValue(this, input.Time) { IsFinal = input.IsFinal };

            var plusValue = Plus.Process(input);
            var minusValue = Minus.Process(input);

            value.Add(Plus, plusValue);
            value.Add(Minus, minusValue);

            if (plusValue.IsEmpty || minusValue.IsEmpty)
                return value;

            var plus = plusValue.ToDecimal();
            var minus = minusValue.ToDecimal();

            var diSum = plus + minus;
            var diDiff = Math.Abs(plus - minus);

            value.Add(this, value.SetValue(this, diSum != 0m ? (100 * diDiff / diSum) : 0m));

            return value;
        }

        public override void Load(SettingsStorage storage)
        {
            base.Load(storage);
            Length = storage.GetValue<int>(nameof(Length));
        }

        public override void Save(SettingsStorage storage)
        {
            base.Save(storage);
            storage.SetValue(nameof(Length), Length);
        }

        public override string ToString() => $"{base.ToString()} {Length}";
    }
}
