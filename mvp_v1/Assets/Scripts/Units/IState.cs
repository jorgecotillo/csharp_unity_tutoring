namespace GoblinSiege.Units
{
    /// <summary>FSM state contract (spec: one controller, states as classes).</summary>
    public interface IState
    {
        void Enter();
        void Execute();
        void Exit();
    }
}
