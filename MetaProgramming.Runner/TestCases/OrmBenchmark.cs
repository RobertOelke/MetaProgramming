using MetaProgramming.Benchmark;
using MetaProgramming.ORM.EFCore;
using MetaProgramming.ORM.Manually;
using System;
using System.Collections.Generic;
using System.Text;
using EfBlog = MetaProgramming.ORM.EFCore.Blog;
using MBlog = MetaProgramming.ORM.Manually.Blog;

namespace MetaProgramming.Runner.TestCases
{
    public class OrmBenchmark
    {
        [BenchmarkTest]
        public void EfCoreInsert()
        {
            using (var db = new BloggingContext())
            {
                db.AddRange(
                    new EfBlog { Url = "http://blogs.msdn.com/adonet" },
                    new EfBlog { Url = "http://blogs.msdn.com/1" },
                    new EfBlog { Url = "http://blogs.msdn.com/2" });

                // db.Add(new EfBlog { Url = "http://blogs.msdn.com/adonet" });
                // db.Add(new EfBlog { Url = "http://blogs.msdn.com/1" });
                // db.Add(new EfBlog { Url = "http://blogs.msdn.com/2" });
                db.SaveChanges();
            }
        }

        [DefaultBenchmarkTest]
        [BenchmarkTest]
        public void ManuallyInsert()
        {
            using (var db = new BlogRepository(BlogRepository.NewConnection()))
            {
                db.Add(new MBlog { Url = "http://blogs.msdn.com/adonet" });
                db.Add(new MBlog { Url = "http://blogs.msdn.com/1" });
                db.Add(new MBlog { Url = "http://blogs.msdn.com/2" });
                //db.Commit();
            }
        }
    }
}
