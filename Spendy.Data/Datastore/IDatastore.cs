namespace Spendy.Data.Datastore
{
    using System;
    using System.Linq.Expressions;

    public interface IDatastore
    {
        T[] FindAll<T>();
        T[] Find<T>(Expression<Func<T, bool>> predicate);
        T FindOne<T>(Expression<Func<T, bool>> predicate);
        Guid Insert<T>(T itemToInsert);
        void InsertMany<T>(T[] itemsToInsert);
        void Update<T>(Guid id, T itemToUpdate);
        void DeleteMany<T>(Expression<Func<T, bool>> predicate);
    }
}
