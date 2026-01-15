namespace Executor.BudgetManager;

public class BudgetManager
{
    public int MaxReplans { get; set; } = 3;
    public int MaxTotalTicks { get; set; } = 4000;
    public int MaxGoldSpend { get; set; } = 400;

    private int _currentReplans = 0;
    private int _startTick = -1;
    private int _startGold = -1;

    public void Reset(int currentTick, int currentGold)
    {
        _currentReplans = 0;
        _startTick = currentTick;
        _startGold = currentGold;
    }

    public void RecordReplan()
    {
        _currentReplans++;
    }

    public bool IsExceeded(int currentTick, int currentGold, out string reason)
    {
        reason = string.Empty;

        if (_currentReplans > MaxReplans)
        {
            reason = $"Replans exceeded limit ({_currentReplans}/{MaxReplans})";
            return true;
        }

        if (_startTick != -1 && (currentTick - _startTick) > MaxTotalTicks)
        {
            reason = $"Ticks exceeded limit ({currentTick - _startTick}/{MaxTotalTicks})";
            return true;
        }

        if (_startGold != -1 && (_startGold - currentGold) > MaxGoldSpend)
        {
            reason = $"Gold spend exceeded limit ({_startGold - currentGold}/{MaxGoldSpend})";
            return true;
        }

        return false;
    }
}
