using SQLite.Net.Interop;

namespace Divisas2.Interfaces
{
    public interface IConfig
    {
        string DirectoryDB { get; }
        ISQLitePlatform Platform { get; }
    }
}
