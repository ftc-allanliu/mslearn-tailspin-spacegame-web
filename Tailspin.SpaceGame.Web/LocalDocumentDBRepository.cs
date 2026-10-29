using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using TailSpin.SpaceGame.Web.Models;

namespace TailSpin.SpaceGame.Web
{
    public class LocalDocumentDBRepository<T> : IDocumentDBRepository<T> where T : Model
    {
        // An in-memory list of all items in the collection.
        private readonly List<T> _items;

        public LocalDocumentDBRepository(string fileName)
        {
            // Serialize the items from the provided JSON document.
            _items = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(fileName));
        }

        public LocalDocumentDBRepository(Stream stream)
        {
            using var reader = new StreamReader(stream);
            _items = JsonSerializer.Deserialize<List<T>>(reader.ReadToEnd());
        }

        /// <summary>
        /// Retrieves the item from the store with the given identifier.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the retrieved item or null if not found.
        /// </returns>
        /// <param name="id">The identifier of the item to retrieve.</param>
        public Task<T> GetItemAsync(string id)
        {
            return Task<T>.FromResult(_items.FirstOrDefault(item => item.Id == id));
        }

        /// <summary>
        /// Retrieves items from the store that match the given query predicate.
        /// Results are given in descending order by the given ordering predicate.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the collection of retrieved items.
        /// </returns>
        /// <param name="queryPredicate">Predicate that specifies which items to select.</param>
        /// <param name="orderDescendingPredicate">Predicate that specifies how to sort the results in descending order.</param>
        /// <param name="page">The 0-based page index of results to return.</param>
        /// <param name="pageSize">The number of items on a page.</param>
        public Task<IEnumerable<T>> GetItemsAsync(
            Func<T, bool> queryPredicate,
            Func<T, int> orderDescendingPredicate,
            int page = 0, int pageSize = 10
        )
        {
            var skipCount = Math.Max(0, page * Math.Max(0, pageSize));
            var takeCount = Math.Max(0, pageSize);

            var result = _items
                .Where(queryPredicate) // filter
                .OrderByDescending(orderDescendingPredicate) // sort
                .Skip(skipCount) // find page
                .Take(takeCount); // take items

            return Task<IEnumerable<T>>.FromResult(result);
        }

        /// <summary>
        /// Retrieves the number of items that match the given query predicate.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the number of items that match the query predicate.
        /// </returns>
        /// <param name="queryPredicate">Predicate that specifies which items to select.</param>
        public Task<int> CountItemsAsync(Func<T, bool> queryPredicate)
        {
            var count = _items.Count(queryPredicate);

            return Task<int>.FromResult(count);
        }
    }
}
