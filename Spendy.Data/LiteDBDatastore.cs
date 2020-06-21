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

        public T[] Find<T>(Expression<Func<T, bool>> predicate)
        {
            using var db = new LiteDatabase(DBPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            return collection.Find(predicate).ToArray();
        }

        public T FindOne<T>(Expression<Func<T, bool>> predicate)
        {
            using var db = new LiteDatabase(DBPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            return collection.FindOne(predicate);
        }

        public ObjectId Insert<T>(T itemToInsert)
        {
            using var db = new LiteDatabase(DBPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            return collection.Insert(itemToInsert);
        }

        public void InsertMany<T>(T[] itemsToInsert)
        {
            using var db = new LiteDatabase(DBPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            collection.Insert(itemsToInsert);
        }

        public void Update<T>(ObjectId id, T itemToUpdate)
        {
            using var db = new LiteDatabase(DBPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            collection.Update(id, itemToUpdate);
        }

        public void DeleteMany<T>(Expression<Func<T, bool>> predicate)
        {
            using var db = new LiteDatabase(DBPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            collection.DeleteMany(predicate);
        }
    }
}
