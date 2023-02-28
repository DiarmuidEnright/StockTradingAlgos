﻿namespace StockSharp.Algo.Candles.Patterns;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Ecng.Serialization;
using Ecng.Collections;

using StockSharp.Localization;
using StockSharp.Messages;

/// <summary>
/// Base complex implementation of <see cref="ICandlePattern"/>.
/// </summary>
public class ComplexCandlePattern : ICandlePattern
{
	private int _curr;

	/// <summary>
	/// Initializes a new instance of the <see cref="ComplexCandlePattern"/>.
	/// </summary>
	public ComplexCandlePattern()
	{
	}

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NameKey,
		Description = LocalizedStrings.NameKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public string Name { get; set; }

	/// <summary>
	/// Inner patterns.
	/// </summary>
	public IList<ICandlePattern> Inner { get; } = new List<ICandlePattern>();

	int ICandlePattern.CandlesCount => Inner.Count;

	void ICandlePattern.Validate()
	{
		foreach (var i in Inner)
			i.Validate();
	}

	void ICandlePattern.Reset()
	{
		_curr = 0;

		foreach (var i in Inner)
			i.Reset();
	}

	bool ICandlePattern.Recognize(ICandleMessage candle)
	{
		var isFinished = candle.State == CandleStates.Finished;

		if (Inner[_curr].Recognize(candle))
		{
			if (isFinished)
			{
				if (++_curr < Inner.Count)
					return false;

				_curr = 0;
				return true;
			}

			return (_curr + 1) == Inner.Count;
		}

		if (isFinished)
			_curr = 0;

		return false;
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Name), Name)
			.Set(nameof(Inner), Inner.Select(i => i.SaveEntire(false)).ToArray())
		;
	}

	void IPersistable.Load(SettingsStorage storage)
	{
		Name = storage.GetValue<string>(nameof(Name));

		Inner.Clear();
		Inner.AddRange(storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Inner)).Select(i => i.LoadEntire<ICandlePattern>()));
	}

	/// <inheritdoc />
	public override string ToString() => Name;
}