namespace Spendy.Data.Datastore
{
    using LiteDB;
    using Spendy.Data.Datastore;
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    public class LiteDBDatastore : IDatastore
    {
        private static readonly string DbPath = @"AppData/Spendy-LiteDB.db";

        public T[] FindAll<T>()
        {
            using var db = new LiteDatabase(DbPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            return collection.FindAll().ToArray();
        }

        public T[] Find<T>(Expression<Func<T, bool>> predicate)
        {
            using var db = new LiteDatabase(DbPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            return collection.Find(predicate).ToArray();
        }

        public T? FindOne<T>(Expression<Func<T, bool>> predicate)
        {
            using var db = new LiteDatabase(DbPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            return collection.FindOne(predicate);
        }

        /// <summary>
        /// Inserts an item. If the entity has a settable Guid Id property it will be
        /// populated before insert; otherwise a new Guid is generated and returned.
        /// </summary>
        public Guid Insert<T>(T itemToInsert)
        {
            var id = Guid.NewGuid();

            var idProp = typeof(T).GetProperty("Id");
            if (idProp?.CanWrite == true)
                idProp.SetValue(itemToInsert, id);

            using var db = new LiteDatabase(DbPath);
            var col = db.GetCollection<T>(typeof(T).Name);

            // Tell LiteDB to use the Guid Id property as the document key.
            col.Insert(new BsonValue(id), itemToInsert);

            return id;
        }

        public void InsertMany<T>(T[] itemsToInsert)
        {
            var idProp = typeof(T).GetProperty("Id");

            using var db = new LiteDatabase(DbPath);
            var col = db.GetCollection<T>(typeof(T).Name);

            db.BeginTrans();
            foreach (var item in itemsToInsert)
            {
                var id = Guid.NewGuid();
                if (idProp?.CanWrite == true)
                    idProp.SetValue(item, id);

                col.Insert(new BsonValue(id), item);
            }
            db.Commit();
        }

        public void Update<T>(Guid id, T itemToUpdate)
        {
            using var db = new LiteDatabase(DbPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            collection.Update(new BsonValue(id), itemToUpdate);
        }

        public void DeleteMany<T>(Expression<Func<T, bool>> predicate)
        {
            using var db = new LiteDatabase(DbPath);
            var collection = db.GetCollection<T>(typeof(T).Name);

            collection.DeleteMany(predicate);
        }
    }
}
