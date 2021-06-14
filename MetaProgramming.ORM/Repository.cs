using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MetaProgramming.ORM
{
    public interface IRepository<T>
    {
        Task Add(T newItem);
        
        Task<List<T>> GetAllAsync();

        Task Update(T newItem);

        Task Delete(T newItem);
    }

    public class Repository<T>
    {
    }
}
