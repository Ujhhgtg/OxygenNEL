using System.Net.Sockets;
using System.Text;

namespace OxygenNEL.IRC;

public class TcpConnection(string host, int port) : IDisposable
{
    private readonly object _lock = new();

    private TcpClient? _tcp;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public bool Connected { get; private set; }

    public void Connect()
    {
        lock (_lock)
        {
            if (Connected) return;
            _tcp = new TcpClient();
            _tcp.Connect(host, port);
            var stream = _tcp.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            Connected = true;
        }
    }

    public void Close()
    {
        lock (_lock)
        {
            Connected = false;
            _writer?.Dispose();
            _reader?.Dispose();
            _tcp?.Dispose();
            _writer = null;
            _reader = null;
            _tcp = null;
        }
    }

    public void WriteLine(string line)
    {
        lock (_lock)
        {
            if (!Connected || _writer == null) return;
            _writer.WriteLine(line);
        }
    }

    public string? ReadLine()
    {
        if (!Connected || _reader == null) return null;
        try
        {
            return _reader.ReadLine();
        }
        catch
        {
            Connected = false;
            return null;
        }
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}
