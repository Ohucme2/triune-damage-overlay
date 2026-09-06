using System.Text;
using System.Text.RegularExpressions;
using TriuneDamageOverlay.Core;

namespace TriuneDamageOverlay;

internal sealed class LogTailer : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 100 };
    private string _path = string.Empty;
    private long _position;
    private string _partial = string.Empty;
    private EverQuestDamageParser _parser = new(string.Empty);

    public event Action<DamageEvent>? Damage;
    public event Action<string>? LineRead;
    public event Action<string>? Status;
    public string CharacterName { get; private set; } = string.Empty;
    public bool Running => _timer.Enabled;

    public LogTailer() => _timer.Tick += (_, _) => Poll();

    public bool Start(string path)
    {
        Stop();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Status?.Invoke("Choose a valid EverQuest log file.");
            return false;
        }

        _path = path;
        CharacterName = ReadCharacterName(path);
        _parser = new EverQuestDamageParser(CharacterName);
        _position = new FileInfo(path).Length; // New damage only; never replay an old session.
        _partial = string.Empty;
        _timer.Start();
        Status?.Invoke($"Watching {CharacterName}'s new combat lines");
        return true;
    }

    public void Stop()
    {
        _timer.Stop();
        _path = string.Empty;
        _position = 0;
        _partial = string.Empty;
    }

    private void Poll()
    {
        try
        {
            if (!File.Exists(_path)) return;
            var length = new FileInfo(_path).Length;
            if (length < _position)
            {
                _position = 0;
                _partial = string.Empty;
            }
            if (length == _position) return;

            using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            stream.Seek(_position, SeekOrigin.Begin);
            var byteCount = checked((int)(stream.Length - _position));
            var bytes = new byte[byteCount];
            var read = stream.Read(bytes, 0, bytes.Length);
            _position += read;
            ProcessText(_partial + Encoding.UTF8.GetString(bytes, 0, read));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { Status?.Invoke("The log file cannot be read."); }
    }

    private void ProcessText(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');
        _partial = lines[^1];
        for (var i = 0; i < lines.Length - 1; i++)
        {
            LineRead?.Invoke(lines[i]);
            if (_parser.TryParse(lines[i], out var damage) && damage is not null)
                Damage?.Invoke(damage);
        }
    }

    internal static string ReadCharacterName(string path)
    {
        var name = Path.GetFileName(path);
        var match = Regex.Match(name, @"^eqlog_(?<character>[^_]+)_.+\.txt$", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["character"].Value : "this character";
    }

    public void Dispose() => _timer.Dispose();
}
