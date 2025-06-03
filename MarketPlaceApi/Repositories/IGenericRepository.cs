using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MarketPlaceApi.Repositories
{
    // كلاس مساعد عشان يحدد العلاقات المتداخلة (Include و ThenInclude)
    public class IncludeExpression<T>
    {
        public Expression<Func<T, object>> Include { get; set; }
        public List<Expression<Func<object, object>>> ThenIncludes { get; set; } = new List<Expression<Func<object, object>>>();
    }

    public interface IGenericRepository<T> where T : class
    {
        Task<T> GetByIdAsync(object id);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<IEnumerable<T>> FindWithIncludeAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes);

        // دالة جديدة تدعم العلاقات المتداخلة (Include و ThenInclude)
        Task<IEnumerable<T>> FindWithNestedIncludeAsync(
            Expression<Func<T, bool>> predicate,
            params IncludeExpression<T>[] includes);
    }
}