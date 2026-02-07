using System.Net.Sockets;
using System.Text;

namespace OxygenNEL.IRC;

public class TcpLineClient(string host, int port) : IDisposable
{
    private TcpClient? _tcp;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public bool Connected => _tcp?.Connected ?? false;

    public void Connect()
    {
        _tcp = new TcpClient();
        _tcp.Connect(host, port);
        var stream = _tcp.GetStream();
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
    }

    public void Send(string line) => _writer?.WriteLine(line);

    public string? Read() => _reader?.ReadLine();

    public void Close()
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _tcp?.Close();
        _tcp?.Dispose();
        _writer = null;
        _reader = null;
        _tcp = null;
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}
