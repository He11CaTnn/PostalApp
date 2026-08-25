using System.Collections.Generic;
using System.Threading.Tasks;

namespace PostalApp
{
    /// <summary>
    /// Ленивая загрузка данных пакетами (постраничная загрузка).
    /// Работает с любой таблицей PostgreSQL через PgQuery&lt;T&gt;.
    /// </summary>
    public class LazyLoader<T> where T : new()
    {
        private readonly SearchFilter<T> _searchEngine;

        private int _batchSize;
        private int _currentCount = 0;

        public bool IsEndOfData { get; private set; } = false;
        public bool IsLoading   { get; private set; } = false;

        public LazyLoader(SearchFilter<T> searchEngine, int batchSize = 50)
        {
            _searchEngine = searchEngine;
            _batchSize    = batchSize;
        }

        public void Reset()
        {
            _currentCount = 0;
            IsEndOfData   = false;
            IsLoading     = false;
        }

        public async Task<List<T>> LoadNextBatchAsync()
        {
            if (IsLoading || IsEndOfData) return new List<T>();

            IsLoading = true;
            try
            {
                int from = _currentCount;
                int to   = _currentCount + _batchSize - 1;

                // Начинаем с базового запроса
                PgQuery<T> query = DataBase._client.From<T>();

                // Применяем активный фильтр поиска
                query = _searchEngine.ApplyToQuery(query);

                // Пагинация и сортировка по id
                query = query
                    .Range(from, to)
                    .Order("id", ascending: true);

                var result  = await query.Get();
                var records = result.Models;

                if (records.Count == 0)
                    IsEndOfData = true;
                else
                    _currentCount += records.Count;

                return records;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public int GetTotalLoaded() => _currentCount;
    }
}
