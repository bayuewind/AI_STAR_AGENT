namespace Tools.WaterBot;

/// <summary>
/// Represents a tile where the player can stand and execute actions.
/// </summary>
public class ActionableTile
{
    private XnaPoint stand;
    private List<XnaPoint> executeOn;
    private Action action;

    public enum Action
    {
        Water,
        Refill,
    }

    public ActionableTile(Action action)
    {
        this.action = action;
        this.executeOn = new List<XnaPoint>();
    }

    public ActionableTile(XnaPoint stand, Action action)
    {
        this.stand = stand;
        this.action = action;
        this.executeOn = new List<XnaPoint>();
    }

    public ActionableTile(XnaPoint stand, List<XnaPoint> executeOn, Action action)
    {
        this.stand = stand;
        this.executeOn = executeOn ?? new List<XnaPoint>();
        this.action = action;
    }

    public void setStand(XnaPoint stand)
    {
        this.stand = stand;
    }

    public XnaPoint getStand()
    {
        return this.stand;
    }

    public Action getAction()
    {
        return this.action;
    }

    public void setExecuteOn(List<XnaPoint> executeOn)
    {
        this.executeOn = executeOn ?? new List<XnaPoint>();
    }

    public void pushExecuteOn(XnaPoint executeOn)
    {
        this.executeOn.Add(executeOn);
    }

    public XnaPoint Pop()
    {
        if (this.executeOn.Count == 0)
        {
            return new XnaPoint(-1, -1);
        }

        XnaPoint temp = this.executeOn[0];
        this.executeOn.RemoveAt(0);
        return temp;
    }

    public bool isDone()
    {
        return this.executeOn.Count == 0;
    }
}
