namespace StockSharp.Algo.Analytics
{
    public class BiggestCandleScript : IAnalyticsScript
    {
        public async Task Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
        {
            if (securities.Length == 0)
            {
                logs.AddWarningLog("No instruments.");
                return;
            }

            var priceChart = panel.CreateChart<DateTimeOffset, decimal, decimal>();
            var volChart = panel.CreateChart<DateTimeOffset, decimal, decimal>();

            var bigPriceCandles = new List<CandleMessage>();
            var bigVolCandles = new List<CandleMessage>();

            var tasks = securities.Select(async security =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                // Get candle storage asynchronously
                var candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format);
                var allCandles = (await Task.Run(() => candleStorage.Load(from, to).ToArray())).Where(c => c != null).ToArray();

                if (allCandles.Length == 0)
                    return;

                // Find the biggest price and volume candles
                CandleMessage bigPriceCandle = null;
                CandleMessage bigVolCandle = null;
                foreach (var candle in allCandles)
                {
                    if (bigPriceCandle == null || candle.GetLength() > bigPriceCandle.GetLength())
                        bigPriceCandle = candle;

                    if (bigVolCandle == null || candle.TotalVolume > bigVolCandle.TotalVolume)
                        bigVolCandle = candle;
                }

                if (bigPriceCandle != null)
                    bigPriceCandles.Add(bigPriceCandle);

                if (bigVolCandle != null)
                    bigVolCandles.Add(bigVolCandle);
            }).ToList();

            // Run all tasks in parallel
            await Task.WhenAll(tasks);

            // Draw series on chart
            if (bigPriceCandles.Any())
            {
                var priceTimes = bigPriceCandles.Select(c => c.OpenTime).ToArray();
                var middlePrices = bigPriceCandles.Select(c => c.GetMiddlePrice(null)).ToArray();
                var priceLengths = bigPriceCandles.Select(c => c.GetLength()).ToArray();
                priceChart.Append("prices", priceTimes, middlePrices, priceLengths);
            }

            if (bigVolCandles.Any())
            {
                var volTimes = bigVolCandles.Select(c => c.OpenTime).ToArray();
                var volMiddlePrices = bigVolCandles.Select(c => c.GetMiddlePrice(null)).ToArray();
                var totalVolumes = bigVolCandles.Select(c => c.TotalVolume).ToArray();
                volChart.Append("volumes", volTimes, volMiddlePrices, totalVolumes);
            }
        }
    }
}
