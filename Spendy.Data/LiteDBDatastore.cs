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

        public void Update<T>(ObjectId id, T itemToUpdate)
        {
            using var db = new LiteDatabase(DBPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            collection.Update(id, itemToUpdate);
        }

        public void Insert<T>(T itemToInsert)
        {
            using var db = new LiteDatabase(DBPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            collection.Insert(itemToInsert);
        }
    }
}
