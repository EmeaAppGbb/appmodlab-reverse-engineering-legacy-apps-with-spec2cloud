using System;
using TransFleet.Data.Repositories;

namespace TransFleet.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> Repository<T>() where T : class;
        int SaveChanges();
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly TransFleetDbContext _context;

        public UnitOfWork(TransFleetDbContext context)
        {
            _context = context;
        }

        public IRepository<T> Repository<T>() where T : class
        {
            return new Repository<T>(_context);
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
