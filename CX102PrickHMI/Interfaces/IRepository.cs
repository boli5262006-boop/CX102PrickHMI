using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CX102PrickHMI.Interfaces
{
    public interface IRepository<T> where T : class
    {
       // T Get(int id);
        //int Update(T entity);
     //   int Delete(T entity);
        int Insert(T entity);
     //   List<T> GetAll();
     //   List<T> GetAll(string keyword);
       // T Select(string keyword);
    }
}
