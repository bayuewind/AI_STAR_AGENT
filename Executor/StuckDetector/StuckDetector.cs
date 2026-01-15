namespace Executor.StuckDetector;

public class StuckDetector
{
    private int _lastTileX = -1;
    private int _lastTileY = -1;
    private string? _lastLocation;
    private int _unchangedTicks = 0;
    private string? _lastErrorCode;
    private int _errorRepeatCount = 0;
    private string? _lastNodeId;
    private int _nodeRepeatCount = 0;

    public int MaxUnchangedTicks { get; set; } = 100;
    public int MaxErrorRepeats { get; set; } = 3;
    public int MaxNodeRepeats { get; set; } = 5;

    public void Update(int x, int y, string location, string? errorCode, string? currentNodeId)
    {
        if (x == _lastTileX && y == _lastTileY && location == _lastLocation)
        {
            _unchangedTicks++;
        }
        else
        {
            _unchangedTicks = 0;
            _lastTileX = x;
            _lastTileY = y;
            _lastLocation = location;
        }

        if (errorCode != null && errorCode == _lastErrorCode)
        {
            _errorRepeatCount++;
        }
        else
        {
            _errorRepeatCount = (errorCode != null) ? 1 : 0;
            _lastErrorCode = errorCode;
        }

        if (currentNodeId != null && currentNodeId != _lastNodeId)
        {
            _lastNodeId = currentNodeId;
            _nodeRepeatCount = 1;
            // When node changes, the error repeat count for the previous node's error is no longer relevant
            _errorRepeatCount = 0;
            _lastErrorCode = null;
        }
    }

    public void RecordNodeVisit(string nodeId)
    {
        if (nodeId == _lastNodeId)
        {
            _nodeRepeatCount++;
        }
        else
        {
            _lastNodeId = nodeId;
            _nodeRepeatCount = 1;
        }
    }

    public bool IsStuck(out string reason, bool ignorePosition = false, bool ignoreErrors = false)
    {
        reason = string.Empty;

        if (!ignorePosition && _unchangedTicks >= MaxUnchangedTicks)
        {
            reason = $"Position unchanged for {_unchangedTicks} ticks";
            return true;
        }

        if (!ignoreErrors && _errorRepeatCount >= MaxErrorRepeats)
        {
            reason = $"Error code '{_lastErrorCode}' repeated {_errorRepeatCount} times";
            return true;
        }

        if (_nodeRepeatCount >= MaxNodeRepeats)
        {
            reason = $"Node '{_lastNodeId}' repeated {_nodeRepeatCount} times";
            return true;
        }

        return false;
    }

    public void Reset()
    {
        _unchangedTicks = 0;
        _errorRepeatCount = 0;
        _nodeRepeatCount = 0;
        _lastTileX = -1;
        _lastTileY = -1;
        _lastErrorCode = null;
        _lastNodeId = null;
    }

    public void ResetErrorCount()
    {
        _errorRepeatCount = 0;
        _lastErrorCode = null;
    }
}
