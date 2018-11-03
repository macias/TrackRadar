using System;
using System.Threading.Tasks;

namespace Geo
{
    public interface IReader : IDisposable
    {
        Task<Way> ReadWayAsync();
    }
}