using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace MetaProgramming.ORM.Manually
{
    public class BlogRepository : IDisposable
    {
        public static Func<IDbConnection> NewConnection { get; } = () =>
        {
            var c = new SqlConnection(@"Data Source=localhost\SQLEXPRESS;Initial Catalog=TestManually;User Id=Test;Password=Test;");
            c.Open();
            return c;
        };

        private IDbConnection _connection;

        private IDbTransaction _transaction;

        public BlogRepository(IDbConnection connection)
        {
            _connection = connection;
            //_transaction = connection.BeginTransaction();
        }

        public void Add(Blog blog)
        {
            var cmd = _connection.CreateCommand();

            cmd.CommandText = @"
                INSERT INTO 
                Blogs (Url)
                values (@URL);
                SELECT SCOPE_IDENTITY();
                ";
            //cmd.Transaction = _transaction;
            cmd.Parameters.Add(new SqlParameter("URL", "Test"));

            var newId = (int)(decimal)cmd.ExecuteScalar();
            blog.Id = newId;
        }

        public void Commit()
        {
            _transaction.Commit();
            _transaction = _connection.BeginTransaction();
        }

        public void Rollback()
        {
            _transaction.Rollback();
            _transaction = _connection.BeginTransaction();
        }

        public void Dispose()
        {
            //_transaction.Rollback();
        }

        // public static void CreateTable(IDbConnection connection)
        // {
        //     var cmd = connection.CreateCommand();
        // 
        //     cmd.CommandText = @"
        //         INSERT INTO 
        //         Blogs ( Url )
        //         values ('Test');
        //         SELECT SCOPE_IDENTITY();
        //         ";
        //     cmd.Transaction = _transaction;
        // 
        //     var newId = (int)cmd.ExecuteScalar();
        // }
    }

    public class Blog
    {
        public int Id { get; set; }
        public string Url { get; set; }

        public List<Post> Posts { get; } = new List<Post>();
    }

    public class Post
    {
        public int PostId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        public int BlogId { get; set; }
        public Blog Blog { get; set; }
    }
}
