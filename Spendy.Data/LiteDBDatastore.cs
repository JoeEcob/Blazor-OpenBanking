namespace Spendy.Data
{
    using LiteDB;
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    public class LiteDBDatastore
    {
        private static string DBPath = @"AppData/Spendy-LiteDB.db";

        public T[] FindAll<T>()
        {
            using var db = new LiteDatabase(DBPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            return collection.FindAll().ToArray();
        }

        public T FindOne<T>(Expression<Func<T, bool>> predicate)
        {
            using var db = new LiteDatabase(DBPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            return collection.FindOne(predicate);
        }

        public void UpdateInCollection<T>(string id, T itemToUpdate)
        {
            using var db = new LiteDatabase(DBPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            // Ensure index creates the collection if it doesn't exist
            collection.EnsureIndex(id); // TODO - is this indexing the value rather than type?

            var exists = collection.Update(id, itemToUpdate);

            if (!exists)
            {
                collection.Insert(id, itemToUpdate);
            }
        }
    }
}
